using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases;
using TcmClinic.Api.UseCases.Profile;
using TcmClinic.Api.Web.Configurations;
using TcmClinic.Api.Web.Services;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// [維運提示] 結構化日誌（Serilog，Principle XVII）；設定與監控關鍵見 LoggerConfigs。
builder.AddLoggerConfigs();

builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();

// 驗證 scheme（JWT Bearer，2026-10-08 起取代 Cookie，ADR-011）於 AddAuthConfigs() 內註冊（SPEC-004，US1）。
// 測試以 CustomWebApplicationFactory 注入測試 scheme。

// 開發環境 CORS：允許前端 dev server（Aspire 編排/Vite）跨來源呼叫（SPEC-003）。
// refresh token 以 HttpOnly Cookie 傳遞，須允許憑證（AllowCredentials），故不可用 AllowAnyOrigin（SPEC-004，ADR-011）。
const string DevCorsPolicy = "DevFrontend";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy(
        DevCorsPolicy,
        policy => policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            // 登入限速回 429 時，前端需讀 Retry-After 提示等待秒數（SPEC-004 FR-006b）。
            .WithExposedHeaders("Retry-After")));
}

builder.Services
    .AddInfrastructureServices(builder.Configuration)
    .AddUseCases()
    .AddMediatorBehaviors()
    .AddAuthConfigs(builder.Configuration, builder.Environment)
    .AddRateLimitConfigs(builder.Configuration)
    .AddProblemDetailsConfigs()
    .AddFastEndpoints()
    .SwaggerDocument();

// 以 HttpContext 版本覆寫 Infrastructure 預設的 SystemCurrentUser：DI 取「最後註冊者」，故 MUST 置於
// AddInfrastructureServices 之後（SPEC-013 修正：原先註冊在前而被覆蓋，使 ICurrentUser 恆為 "system"，
// 稽核欄位、訪問日誌與「本人」判定皆無法取得真實登入帳號）。
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// 密碼效期設定（SPEC-013，FR-011）：綁定 PasswordPolicy 區段，以 singleton 注入用例（用例層不依賴 Options 框架）。
// 啟動時驗證（ValidDays 須介於 1～3650），設定錯誤即無法啟動，避免剩餘天數計算失真。
builder.Services.AddOptions<PasswordPolicyOptions>()
    .Bind(builder.Configuration.GetSection(PasswordPolicyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<PasswordPolicyOptions>>().Value);

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseSerilogRequestLogging(LoggerConfigs.ConfigureRequestLogging);

// 所有錯誤回應一律 ProblemDetails（DEF-011）：須在併發攔截、驗證／授權與端點之前。
app.UseProblemDetailsConfigs();

// SPEC-011／012（第二道防線）：樂觀併發權杖（rowversion）於極短競態窗擲 DbUpdateConcurrencyException，
// 對映為 409 ProblemDetails，避免回 500。此處為全域處理（信用狀受理／退件、角色設定權限皆會觸發），
// 故訊息採通用語意；各功能專屬訊息由用例層第一道防線（Result.Conflict）提供。
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DbUpdateConcurrencyException)
    {
        await Results
            .Problem(statusCode: StatusCodes.Status409Conflict, title: "資源狀態衝突", detail: "資料已被他人變更，請重新載入後再試。")
            .ExecuteAsync(context);
    }
});

// 開發環境 CORS（SPEC-003）：須在驗證/端點之前。
if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);

    // 僅開發環境曝露 API 文件。OpenAPI JSON 由中介層（NSwag）服務、非 endpoint，
    // 無法個別掛 AllowAnonymous；且全域 deny-by-default FallbackPolicy（Principle XVIII）
    // 連「無對應 endpoint」的請求也會擋。故 swagger 文件 MUST 在授權中介「之前」服務，
    // 否則未登入瀏覽器抓 /swagger/v1/swagger.json 會得 401（Scalar 顯示 Document 'v1' could not be loaded）。
    app.UseSwaggerGen();
}

// 登入限速（SPEC-004 FR-006b）：須在路由之後（WebApplication 自動加入）、端點之前；超限的登入請求在驗證前即被擋下。
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(config => config.Errors.UseValidationProblem());

// Scalar API 參考 UI（僅開發；讀上方在授權前服務的 OpenAPI 文件）；UI 位於 /scalar。
// 此為 endpoint，以 AllowAnonymous 放行不受 FallbackPolicy 阻擋。正式環境不曝露文件與 UI。
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
        options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"))
       .AllowAnonymous();
}

// 開發環境：套用 migration 並種入超級管理員（T010）。
// [維運提示] 此種子區塊僅於 Development 執行，正式環境不會落地硬編碼預設密碼（Principle XVIII 機密）。
//   若日後需於非 Development 種子，MUST 以設定 Seed:AdminPassword 供應密碼並強制首次登入改密；
//   下方 "Admin123!" 僅為本機開發便利用的 fallback，切勿用於任何對外環境。
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var initialAdminPassword = app.Configuration["Seed:AdminPassword"] ?? "Admin123!";
    await context.Database.MigrateAsync();
    await SeedData.SeedAsync(context, passwordHasher, initialAdminPassword);
}

app.Run();

// 供 FunctionalTests 的 WebApplicationFactory 使用
public partial class Program;
