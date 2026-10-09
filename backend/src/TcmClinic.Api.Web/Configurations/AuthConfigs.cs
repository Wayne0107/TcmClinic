using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TcmClinic.Api.UseCases.Auth;
using TcmClinic.Api.Web.Authorization;
using TcmClinic.Api.Web.Services;

namespace TcmClinic.Api.Web.Configurations;

/// <summary>
/// JWT Bearer 驗證（SPEC-004 FR-021～FR-021c，ADR-011；2026-10-08 前為 Cookie 驗證）+ 選單導向授權（005）。
/// 授權不再有超級管理員特例；改由每請求寫入的 <c>menu</c> claims（使用者可見選單 route）搭配 <c>MenuAccess:{route}</c> 原則判定。
/// </summary>
public static class AuthConfigs
{
    public static IServiceCollection AddAuthConfigs(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // 簽章金鑰：非 Development 必須設定且合法（啟動時驗證，設定錯誤即無法啟動）；Development 未設定時用隨機金鑰。
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => string.IsNullOrEmpty(o.SigningKey) ? environment.IsDevelopment() : JwtOptions.IsValidKey(o.SigningKey),
                "Jwt:SigningKey 必須為 Base64 且至少 32 bytes；非 Development 環境必須設定。")
            .ValidateOnStart();
        services.AddSingleton<JwtSigningKey>();
        services.AddSingleton<AccessTokenIssuer>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSigningKey, IOptions<JwtOptions>>((options, signingKey, jwt) =>
            {
                // 保留 JWT 原始 claim 名稱（sub、name、sid），驗證後再依資料庫重建身分（OnTokenValidated）。
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey.Key,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtRegisteredClaimNames.Name,
                };
                // R8＋FR-021c：每請求檢查 session 有效，並依資料庫重建角色與可見選單。
                options.Events = new JwtBearerEvents { OnTokenValidated = OnTokenValidatedAsync };
            });

        // 縱深防禦（Principle XVIII deny-by-default）：未明示授權中介的端點一律要求已登入。
        // FastEndpoints 已對其端點預設要求驗證；此 FallbackPolicy 再涵蓋原生對映的端點，
        // 避免日後有人以 MapGet 等方式意外開放。基礎端點（health/scalar）另以 AllowAnonymous 例外。
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // 選單導向授權：動態原則供應器 + 需求處理器（005）。
        services.AddSingleton<IAuthorizationPolicyProvider, MenuAccessPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, MenuAccessHandler>();

        return services;
    }

    /*
     * [維運提示] 每請求依 session 與帳號自 DB 重驗並刷新授權資料（005 授權來源、ADR-011）。
     * 監控關鍵：session 已撤銷／逾期、使用者被刪除／移除全部角色 → Fail（該請求 401）。
     * 每請求刷新：角色代碼 + 可見選單 route（menu claims）——角色可見性變更於下次請求即生效；token 本身不含角色。
     * 應變：若查詢成為熱點，可短暫快取 session 狀態與角色（介於即時性與成本）。
     */
    private static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        var account = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Name);
        var sessionText = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sid);
        var userText = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(account)
            || !Guid.TryParse(sessionText, out var sessionId)
            || !Guid.TryParse(userText, out var userPublicId))
        {
            context.Fail("access token 缺少帳號、使用者或 session。");
            return;
        }

        var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();
        if (!await mediator.Send(new IsSessionActiveQuery(sessionId, userPublicId)))
        {
            // 已登出、被撤銷、逾期，或 session 不屬於 token 的使用者（FR-005、FR-021c）。
            context.Fail("session 已失效。");
            return;
        }

        var result = await mediator.Send(new GetUserRolesByAccountQuery(account));
        if (!result.Exists || result.RoleCodes.Count == 0)
        {
            context.Fail("帳號已刪除或無有效角色。");
            return;
        }

        var routes = await mediator.Send(new GetVisibleMenuRoutesQuery(result.RoleCodes));

        // 以資料庫現時資料重建身分：帳號、顯示名稱（SPEC-013 R12，改名後 /auth/me 立即反映）、角色與可見選單 route。
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account),
            new(JwtRegisteredClaimNames.Sid, sessionId.ToString()),
        };
        if (result.DisplayName is not null)
        {
            claims.Add(new Claim(ClaimTypes.GivenName, result.DisplayName));
        }

        claims.AddRange(result.RoleCodes.Select(code => new Claim(ClaimTypes.Role, code)));
        claims.AddRange(routes.Select(route => new Claim(MenuAccessHandler.MenuClaimType, route)));
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            claims, JwtBearerDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role));
    }
}
