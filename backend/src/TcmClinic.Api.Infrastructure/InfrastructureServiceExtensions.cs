using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Auditing;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Auth;

namespace TcmClinic.Api.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // 預設當前使用者；Web 層可用 HttpContext 版本覆寫（AddScoped 之後註冊者優先）。
        services.AddSingleton<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AuditLogInterceptor>();

        // 資料訪問記錄服務（SPEC-010）：旁路、盡力寫入，供敏感讀取/匯出端點呼叫（FR-015）。
        services.AddScoped<IAccessLogRecorder, AccessLogRecorder>();

        // 使用者操作紀錄（SPEC-013）：獨立 DbContext、盡力寫入，成功與失敗皆記（FR-015）。
        services.AddScoped<IUserLogRecorder, UserLogRecorder>();

        // 密碼雜湊（加鹽 PBKDF2，FR-007、ADR-009）與登入失敗鎖定（行程內記憶體，FR-006a）。
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILoginThrottle, MemoryLoginThrottle>();
        // session 輪替的條件式保存（SPEC-004 FR-021b）：與請求共用 AppDbContext。
        services.AddScoped<ISessionRotationStore, SessionRotationStore>();

        // 正式環境 MUST 由設定/User Secrets/環境變數提供 DefaultConnection（含加密與有效憑證，
        // Principle XVIII 加密）。下方僅為本機 localhost 開發 fallback：loopback 不需 TLS，故用
        // Encrypt=False，而非以 TrustServerCertificate=True 停用憑證驗證（後者會被弱掃標記）。
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=TcmClinicApi;Trusted_Connection=True;Encrypt=False";

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString);
            // 順序重要：Auditable 先（填 Modify*、轉軟刪除），AuditLog 後（快照含最終值並正確分類）。
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<AuditLogInterceptor>());
        });

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

        return services;
    }
}
