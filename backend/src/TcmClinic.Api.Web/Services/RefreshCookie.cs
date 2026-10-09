using Microsoft.AspNetCore.Http;

namespace TcmClinic.Api.Web.Services;

/// <summary>
/// refresh token 的 HttpOnly Cookie（SPEC-004 FR-021，ADR-011）：非 Development 環境 Secure、SameSite=Lax、Path=/，
/// 到期時間同 session。前端 JavaScript 讀不到，瀏覽器呼叫 /auth/refresh、/auth/logout 時自動帶上。
/// </summary>
public static class RefreshCookie
{
    public const string Name = "TcmClinic.Api.Refresh";

    /// <summary>讀取請求中的 refresh token（沒有時為 null）。</summary>
    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    /// <summary>設定 refresh Cookie；<paramref name="expiresAt"/> 為本地時間。</summary>
    public static void Write(HttpContext context, string refreshToken, DateTime expiresAt) =>
        context.Response.Cookies.Append(Name, refreshToken, Options(context, new DateTimeOffset(expiresAt)));

    /// <summary>清除 refresh Cookie（以過去時間覆寫）。</summary>
    public static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(Name, Options(context, null));

    private static CookieOptions Options(HttpContext context, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        // 正式環境一律只經 HTTPS 傳送；開發環境走 Aspire DCP 的純 HTTP，故依請求決定（同原登入 Cookie）。
        Secure = !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment() || context.Request.IsHttps,
        Expires = expires,
    };
}
