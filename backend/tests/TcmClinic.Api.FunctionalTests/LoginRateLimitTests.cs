using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using static TcmClinic.Api.FunctionalTests.ProfileTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 登入限速測試工廠：每個來源 IP 每分鐘只允許 3 次登入請求（正式預設 20 次），以標頭 <see cref="ClientIpHeader"/>
/// 模擬不同來源 IP（TestServer 的連線沒有 IP）。使用真實 Cookie 驗證。
/// </summary>
public class LoginRateLimitFactory : AuthWebApplicationFactory
{
    public const string ClientIpHeader = "X-Test-Client-Ip";

    public const int PermitLimit = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("LoginRateLimit:PermitLimit", PermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, ClientIpStartupFilter>());
    }

    /// <summary>於管線最前端依測試標頭設定連線的來源 IP。</summary>
    private sealed class ClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(ClientIpHeader, out var ip))
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
                }

                await nextMiddleware();
            });
            next(app);
        };
    }
}

/// <summary>
/// 登入端點限速（SPEC-004 FR-006b，2026-10-08 需求變更，DEF-017）：同一來源 IP 在時間窗內超過上限的登入請求回 429
/// ProblemDetails 並附 Retry-After；不同來源 IP 分開計算；被限速的請求不進入驗證，不計入帳號失敗次數（FR-006a）。
/// 每個測試使用不同的來源 IP，避免共用工廠時互相影響。需 Docker Desktop。
/// </summary>
public class LoginRateLimitTests(LoginRateLimitFactory factory) : IClassFixture<LoginRateLimitFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpResponseMessage> LoginFromAsync(string ip, string account, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new { account, password }),
        };
        request.Headers.Add(LoginRateLimitFactory.ClientIpHeader, ip);
        return await factory.CreateClient().SendAsync(request, Ct);
    }

    // 情境：同一 IP 已用完時間窗內的 3 次額度，第 4 次登入請求回 429 ProblemDetails，並以 Retry-After 告知幾秒後可再試。
    // @SPEC-004 @AC-32
    [Fact(DisplayName = "同一 IP 超過登入次數上限回 429 並附 Retry-After（SPEC-004 FR-006b）")]
    public async Task Exceeding_limit_returns_429_with_retry_after()
    {
        for (var i = 0; i < LoginRateLimitFactory.PermitLimit; i++)
        {
            var allowed = await LoginFromAsync("10.0.0.1", "nobody", "wrong");
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var res = await LoginFromAsync("10.0.0.1", "nobody", "wrong");

        Assert.Equal(HttpStatusCode.TooManyRequests, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
        Assert.True(int.Parse(res.Headers.GetValues("Retry-After").Single(), System.Globalization.CultureInfo.InvariantCulture) > 0);
    }

    // 情境：某個 IP 被限速時，其他 IP 的登入不受影響（依來源 IP 分開計算）。
    // @SPEC-004 @AC-32
    [Fact(DisplayName = "不同來源 IP 的登入次數分開計算（SPEC-004 FR-006b）")]
    public async Task Limit_is_per_client_ip()
    {
        for (var i = 0; i <= LoginRateLimitFactory.PermitLimit; i++)
        {
            await LoginFromAsync("10.0.0.2", "nobody", "wrong");
        }

        var other = await LoginFromAsync("10.0.0.3", "nobody", "wrong");

        Assert.Equal(HttpStatusCode.Unauthorized, other.StatusCode);
    }

    // 情境：被限速擋下的請求不進入驗證，不累計帳號連續失敗次數——3 次失敗後再被擋 5 次，
    // 若被擋的也計入就會達到 5 次而鎖定；換 IP 以正確密碼登入仍成功（FR-006a 鎖定門檻 5 次）。
    // @SPEC-004 @AC-32
    [Fact(DisplayName = "被限速的登入不計入帳號失敗次數（SPEC-004 FR-006b、FR-006a）")]
    public async Task Rejected_requests_do_not_count_toward_lockout()
    {
        await CreateUserAsync(factory, "ratelimit01");
        for (var i = 0; i < LoginRateLimitFactory.PermitLimit; i++)
        {
            await LoginFromAsync("10.0.0.4", "ratelimit01", "wrong");
        }

        for (var i = 0; i < 5; i++)
        {
            var blocked = await LoginFromAsync("10.0.0.4", "ratelimit01", "wrong");
            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        }

        var res = await LoginFromAsync("10.0.0.5", "ratelimit01", Password);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
