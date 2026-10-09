using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TcmClinic.Api.Core.SessionAggregate;
using static TcmClinic.Api.FunctionalTests.AuthTestSupport;
using static TcmClinic.Api.FunctionalTests.ProfileTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// JWT Bearer 登入（SPEC-004 FR-021～FR-021c，ADR-011）：access token 15 分鐘、refresh token 以 HttpOnly Cookie 傳遞並輪替、
/// 重用偵測、session 逾期與帳號失效。用戶端不自動處理 Cookie，refresh token 由測試自行帶入。需 Docker Desktop。
/// </summary>
public class JwtAuthTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    /// <summary>建立帳號並登入，回傳帶 Bearer 的用戶端、登入回應與 refresh token。</summary>
    private async Task<(HttpClient Client, LoginBody Body, string Refresh)> LoginNewUserAsync(string account)
    {
        await CreateUserAsync(factory, account);
        var client = NewClient();
        var res = await PostLoginAsync(client, account, Password, Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (client, (await res.Content.ReadFromJsonAsync<LoginBody>(Ct))!, RefreshTokenOf(res)!);
    }

    private static Guid SessionIdOf(string accessToken) =>
        Guid.Parse(new JsonWebTokenHandler().ReadJsonWebToken(accessToken).GetClaim("sid").Value);

    private Task<UserSession> SessionAsync(Guid sessionId) =>
        QueryDbAsync(factory, ctx => ctx.UserSessions.AsNoTracking()
            .SingleAsync(s => s.PublicId == UserSessionPublicId.From(sessionId)));

    /// <summary>以應用程式本地時間改寫 session 的時間欄位（容器時鐘為 UTC，不可用 SQL 的 GETDATE）。</summary>
    private Task SetSessionTimeAsync(Guid sessionId, string column, DateTime value) =>
        WithDbAsync(factory, ctx => column switch
        {
            "ExpiresAt" => ctx.Database.ExecuteSqlAsync(
                $"UPDATE SysUserSession SET ExpiresAt = {value} WHERE PublicId = {sessionId}"),
            "LastRotatedAt" => ctx.Database.ExecuteSqlAsync(
                $"UPDATE SysUserSession SET LastRotatedAt = {value} WHERE PublicId = {sessionId}"),
            _ => throw new ArgumentOutOfRangeException(nameof(column)),
        });

    private async Task<HttpStatusCode> MeStatusAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return (await NewClient().SendAsync(request, Ct)).StatusCode;
    }

    // 情境：登入回應以 Set-Cookie 設定 refresh Cookie，屬性為 HttpOnly、SameSite=Lax、Path=/；
    // 測試為 Development 環境，不帶 Secure（非 Development 由 Code Review 確認）。
    // @SPEC-004 @AC-24
    [Fact(DisplayName = "refresh Cookie 為 HttpOnly、SameSite=Lax、Path=/（SPEC-004 FR-021）")]
    public async Task Refresh_cookie_is_http_only_lax()
    {
        await CreateUserAsync(factory, "jwt01");
        var res = await PostLoginAsync(NewClient(), "jwt01", Password, Ct);

        var cookie = RefreshSetCookie(res)!.ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=lax", cookie);
        Assert.Contains("path=/", cookie);
    }

    // 情境：access token 效期 15 分鐘（expiresIn 900、exp−iat=900），帶 sub／name／sid，不含角色（每請求由資料庫取得）。
    // @SPEC-004 @AC-24
    [Fact(DisplayName = "access token 效期 15 分鐘且不含角色（SPEC-004 FR-021）")]
    public async Task Access_token_expires_in_15_minutes_without_roles()
    {
        var (_, body, _) = await LoginNewUserAsync("jwt02");
        var token = new JsonWebTokenHandler().ReadJsonWebToken(body.AccessToken);

        Assert.Equal("Bearer", body.TokenType);
        Assert.Equal(900, body.ExpiresIn);
        Assert.Equal(TimeSpan.FromMinutes(15), token.ValidTo - token.IssuedAt);
        Assert.Equal("jwt02", token.GetClaim("name").Value);
        Assert.True(token.TryGetClaim("sid", out _));
        Assert.True(token.TryGetClaim("sub", out _));
        Assert.DoesNotContain(token.Claims, c => c.Type is "role" or ClaimTypes.Role);
    }

    // 情境：以 refresh Cookie 換新 → 200、新的 access token 可用、refresh Cookie 換成新值，session 到期時間延長為現在＋8 小時。
    // @SPEC-004 @AC-33
    [Fact(DisplayName = "refresh 換發新 token 並輪替 Cookie、到期時間滑動（SPEC-004 FR-021a）")]
    public async Task Refresh_rotates_tokens_and_slides_expiry()
    {
        var (client, body, refresh) = await LoginNewUserAsync("jwt03");
        var sessionId = SessionIdOf(body.AccessToken);
        await SetSessionTimeAsync(sessionId, "ExpiresAt", DateTime.Now.AddHours(1));

        var res = await RefreshWithAsync(client, refresh, Ct);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var renewed = (await res.Content.ReadFromJsonAsync<LoginBody>(Ct))!;
        var newRefresh = RefreshTokenOf(res);
        Assert.NotNull(newRefresh);
        Assert.NotEqual(refresh, newRefresh);
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(renewed.AccessToken));
        var session = await SessionAsync(sessionId);
        Assert.InRange(session.ExpiresAt, DateTime.Now.AddHours(8).AddMinutes(-1), DateTime.Now.AddHours(8).AddMinutes(1));
    }

    // 情境：已輪替掉的 refresh token 在 30 秒寬限後又被出示 → 視為外洩：401，整個 session 撤銷，新的 refresh token 與 access token 都失效。
    // @SPEC-004 @AC-34
    [Fact(DisplayName = "寬限後重用舊 refresh token 會撤銷整個 session（SPEC-004 FR-021b）")]
    public async Task Reused_refresh_token_revokes_session()
    {
        var (client, body, refresh) = await LoginNewUserAsync("jwt04");
        var sessionId = SessionIdOf(body.AccessToken);
        var rotated = await RefreshWithAsync(client, refresh, Ct);
        var newRefresh = RefreshTokenOf(rotated)!;
        var newAccess = (await rotated.Content.ReadFromJsonAsync<LoginBody>(Ct))!.AccessToken;
        await SetSessionTimeAsync(sessionId, "LastRotatedAt", DateTime.Now.AddMinutes(-1));

        var reused = await RefreshWithAsync(client, refresh, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshWithAsync(client, newRefresh, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(newAccess));
        Assert.Equal(UserSession.RevokeReasons.Reuse, (await SessionAsync(sessionId)).RevokeReason);
    }

    // 情境：多分頁同時換新——已輪替掉的 refresh token 在 30 秒內又被出示 → 200 只換發 access token、不設定新 Cookie；新的 refresh token 仍可用。
    // @SPEC-004 @AC-35
    [Fact(DisplayName = "寬限內重用舊 refresh token 只換發 access token（SPEC-004 FR-021b）")]
    public async Task Previous_refresh_token_within_grace_returns_access_token()
    {
        var (client, _, refresh) = await LoginNewUserAsync("jwt05");
        var rotated = await RefreshWithAsync(client, refresh, Ct);
        var newRefresh = RefreshTokenOf(rotated)!;

        var concurrent = await RefreshWithAsync(client, refresh, Ct);

        Assert.Equal(HttpStatusCode.OK, concurrent.StatusCode);
        Assert.Null(RefreshSetCookie(concurrent));
        var access = (await concurrent.Content.ReadFromJsonAsync<LoginBody>(Ct))!.AccessToken;
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(access));
        Assert.Equal(HttpStatusCode.OK, (await RefreshWithAsync(client, newRefresh, Ct)).StatusCode);
    }

    // 情境：以合法 session 的身分自行產生 token，但已過期、以其他金鑰簽章，或對象不符 → 一律 401。
    // @SPEC-004 @AC-36
    [Theory(DisplayName = "過期、簽章或對象不符的 access token 回 401（SPEC-004 FR-021c）")]
    [InlineData("expired")]
    [InlineData("wrong-key")]
    [InlineData("wrong-audience")]
    public async Task Invalid_access_token_is_rejected(string variant)
    {
        var (_, body, _) = await LoginNewUserAsync("jwt06" + variant.Replace("-", string.Empty));
        var original = new JsonWebTokenHandler().ReadJsonWebToken(body.AccessToken);
        var key = variant == "wrong-key"
            ? new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            : new SymmetricSecurityKey(Convert.FromBase64String(AuthWebApplicationFactory.SigningKey));
        var now = DateTime.UtcNow;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = original.Issuer,
            Audience = variant == "wrong-audience" ? "someone-else" : original.Audiences.Single(),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = original.Subject,
                ["name"] = original.GetClaim("name").Value,
                ["sid"] = original.GetClaim("sid").Value,
            },
            IssuedAt = variant == "expired" ? now.AddMinutes(-30) : now,
            NotBefore = variant == "expired" ? now.AddMinutes(-30) : now,
            Expires = variant == "expired" ? now.AddMinutes(-15) : now.AddMinutes(15),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });

        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(token));
    }

    // 情境：session 逾期 → refresh 回 401 並以過去時間清除 Cookie；該 session 的 access token 也回 401。
    // @SPEC-004 @AC-37
    [Fact(DisplayName = "session 逾期後 refresh 與 access token 皆回 401（SPEC-004 FR-021a、FR-021c）")]
    public async Task Expired_session_is_rejected()
    {
        var (client, body, refresh) = await LoginNewUserAsync("jwt07");
        await SetSessionTimeAsync(SessionIdOf(body.AccessToken), "ExpiresAt", DateTime.Now.AddMinutes(-1));

        var res = await RefreshWithAsync(client, refresh, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Contains("expires=thu, 01 jan 1970", RefreshSetCookie(res)!.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(body.AccessToken));
    }

    // 情境：帳號被刪除後重建同名帳號 → 舊 access token 的 session 屬於已刪除的帳號（sub 不同），即使未過期也回 401，
    // 不得帶著新帳號的權限通過。
    // @SPEC-004 @AC-40
    [Fact(DisplayName = "同名帳號重建後舊 access token 回 401（SPEC-004 FR-021c）")]
    public async Task Access_token_rejected_after_account_recreated()
    {
        var (_, body, _) = await LoginNewUserAsync("jwt09");
        await WithDbAsync(factory, ctx => ctx.Database.ExecuteSqlRawAsync(
            "UPDATE SysUser SET Removed = 1 WHERE Account = {0}", "jwt09"));
        await CreateUserAsync(factory, "jwt09");

        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(body.AccessToken));
    }

    // 情境：登入中帳號被刪除（軟刪除）→ refresh 回 401，session 以帳號失效撤銷。
    // @SPEC-004 @AC-38
    [Fact(DisplayName = "帳號刪除後 refresh 回 401 並撤銷 session（SPEC-004 FR-021a）")]
    public async Task Refresh_rejected_for_deleted_user()
    {
        var (client, body, refresh) = await LoginNewUserAsync("jwt08");
        await WithDbAsync(factory, ctx => ctx.Database.ExecuteSqlRawAsync(
            "UPDATE SysUser SET Removed = 1 WHERE Account = {0}", "jwt08"));

        var res = await RefreshWithAsync(client, refresh, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal(UserSession.RevokeReasons.UserInvalid, (await SessionAsync(SessionIdOf(body.AccessToken))).RevokeReason);
    }
}
