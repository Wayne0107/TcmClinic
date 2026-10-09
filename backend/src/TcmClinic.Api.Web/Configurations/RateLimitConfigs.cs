using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace TcmClinic.Api.Web.Configurations;

/// <summary>登入限速設定（SPEC-004 FR-006b）：設定區段 <c>LoginRateLimit</c>。</summary>
public sealed class LoginRateLimitOptions
{
    public const string SectionName = "LoginRateLimit";

    /// <summary>每個來源 IP 在一個時間窗內允許的登入請求數（預設 20）。</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 20;

    /// <summary>時間窗長度（秒，預設 60）。</summary>
    [Range(1, 86400)]
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// 登入端點限速（SPEC-004 FR-006b，DEF-017）：依來源 IP 以固定時間窗計數，超過上限回 429 ProblemDetails 並附
/// <c>Retry-After</c>。被擋下的請求不進入登入驗證，故不計入帳號連續失敗次數（FR-006a 的帳號鎖定另行運作）。
/// 只套用於掛 <see cref="LoginPolicy"/> 的端點，不做全站 IP 節流。
/// </summary>
public static class RateLimitConfigs
{
    public const string LoginPolicy = "login";

    public static IServiceCollection AddRateLimitConfigs(this IServiceCollection services, IConfiguration configuration)
    {
        // 啟動時驗證，設定錯誤即無法啟動。
        services.AddOptions<LoginRateLimitOptions>()
            .Bind(configuration.GetSection(LoginRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(LoginPolicy, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<LoginRateLimitOptions>>().Value;
                // 來源 IP 與請求日誌 source_ip 同一來源（部署於 IIS 同程序，即用戶端 IP）。
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                    QueueLimit = 0,
                });
            });

            options.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                // 安全事件：記錄來源 IP 供追查撞庫或密碼噴灑（不記帳號與密碼）。
                http.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimitConfigs).FullName!)
                    .LogWarning("[LOGIN_RATE_LIMITED] 來源 IP {SourceIp} 登入請求超過限速上限", http.Connection.RemoteIpAddress?.ToString());

                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "登入嘗試過於頻繁",
                        detail: "登入請求次數過多，請稍後再試。")
                    .ExecuteAsync(http);
            };
        });

        return services;
    }
}
