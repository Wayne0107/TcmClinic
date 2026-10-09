using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// JWT 登入（SPEC-004 FR-021，ADR-011）的功能測試共用工具：登入後替用戶端帶上 Bearer、讀取 refresh Cookie。
/// </summary>
internal static class AuthTestSupport
{
    /// <summary>refresh Cookie 名稱（ADR-011）。</summary>
    public const string RefreshCookie = "TcmClinic.Api.Refresh";

    /// <summary>POST /auth/login 回應（SPEC-004 API 契約，2026-10-08）。</summary>
    public sealed record LoginBody(
        string AccessToken, string TokenType, int ExpiresIn, string Account, string DisplayName, string[] Roles);

    /// <summary>以帳號密碼登入；成功時替 <paramref name="client"/> 設定 Bearer 標頭。回傳原始回應供斷言。</summary>
    public static async Task<HttpResponseMessage> PostLoginAsync(
        HttpClient client, string account, string password, CancellationToken ct = default)
    {
        var res = await client.PostAsJsonAsync("/auth/login", new { account, password }, ct);
        if (res.IsSuccessStatusCode)
        {
            // 以字串讀取（內容會被緩衝、不關閉串流），呼叫端之後仍可再讀取回應本文。
            var body = JsonSerializer.Deserialize<LoginBody>(await res.Content.ReadAsStringAsync(ct), JsonSerializerOptions.Web);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        }

        return res;
    }

    /// <summary>取回應中 refresh Cookie 的 Set-Cookie 標頭（沒有時為 null）。</summary>
    public static string? RefreshSetCookie(HttpResponseMessage res) =>
        res.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(RefreshCookie + "=", StringComparison.Ordinal))
            : null;

    /// <summary>取 Set-Cookie 中的 refresh token 值（沒有時為 null）。</summary>
    public static string? RefreshTokenOf(HttpResponseMessage res) =>
        RefreshSetCookie(res) is { } header
            ? header[(RefreshCookie.Length + 1)..].Split(';')[0]
            : null;

    /// <summary>以指定 refresh token 呼叫 POST /auth/refresh（自行帶 Cookie，不經用戶端的 Cookie 容器）。</summary>
    public static async Task<HttpResponseMessage> RefreshWithAsync(
        HttpClient client, string refreshToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookie}={refreshToken}");
        return await client.SendAsync(request, ct);
    }
}
