using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using static TcmClinic.Api.FunctionalTests.AuthTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>登入／登出／未授權的功能測試（US1；2026-10-08 起為 JWT Bearer＋refresh Cookie，ADR-011）。</summary>
public class AuthEndpointsTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境：未登入存取受保護端點 → 401（而非 302 轉頁，FR-001、R1）。
    // @SPEC-004 @AC-1
    // @SPEC-005 @AC-4
    [Fact(DisplayName = "未登入存取受保護端點回應 401 而非 302 轉頁（FR-001、R1）")]
    public async Task Protected_endpoint_without_login_returns_401()
    {
        var client = factory.CreateClient();
        var res = await client.GetAsync("/users", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // 情境：正確帳密 → 200，回應含 Bearer access token 並設定 refresh Cookie；以 Bearer 可存取受保護端點（FR-002/004、FR-021）。
    // @SPEC-004 @AC-2
    [Fact(DisplayName = "正確帳密登入回應 access token 與 refresh Cookie 且可存取受保護端點（FR-002、FR-021）")]
    public async Task Login_success_grants_access()
    {
        var client = factory.CreateClient();
        var login = await PostLoginAsync(client, "admin", "Admin123!", Ct);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<LoginBody>(Ct);
        Assert.Equal("Bearer", body!.TokenType);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.NotNull(RefreshTokenOf(login));

        var users = await client.GetAsync("/users", Ct);
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
    }

    // SPEC-008：/me/menus 回應為精簡形狀 { text, value, subMenu? }；value 即後端 Route（如 accountMaintenance）。
    private sealed record MenuNode(string Text, string? Value, MenuNode[]? SubMenu);

    private static IEnumerable<MenuNode> Flatten(IEnumerable<MenuNode> nodes) =>
        nodes.SelectMany(n => new[] { n }.Concat(Flatten(n.SubMenu ?? [])));

    // 情境（005 T032）：種子 admin 登入後即見管理選單且可存取各頁（開箱即用，種子提供初始可見性）；
    // 使用者與角色端點由「帳號維護」／「角色權限維護」選單授權。
    // @SPEC-004 @AC-4
    // @SPEC-004 @AC-30
    // @SPEC-005 @AC-11
    // @SPEC-006 @AC-13
    // @SPEC-008 @AC-1
    // @SPEC-008 @AC-6
    // @SPEC-008 @AC-11
    // @SPEC-010 @AC-14
    [Fact(DisplayName = "種子 admin 登入後即見管理選單且可存取各頁（005 開箱即用）")]
    public async Task Seeded_admin_sees_menus_and_can_access()
    {
        var client = factory.CreateClient();
        (await PostLoginAsync(client, "admin", "Admin123!", Ct)).EnsureSuccessStatusCode();

        var menus = await client.GetFromJsonAsync<MenuNode[]>("/me/menus", Ct);
        Assert.NotNull(menus);
        // SPEC-008：改讀 value（= 後端 Route）。
        var routes = Flatten(menus!).Select(m => m.Value).ToHashSet();
        Assert.Contains("/admin/menus", routes);
        Assert.Contains("accountMaintenance", routes);
        Assert.Contains("rolePermission", routes);
        foreach (var retired in new[] { "/admin/users", "/admin/roles", "/admin/audit-logs", "/admin/access-logs" })
        {
            Assert.DoesNotContain(retired, routes);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/users?page=1&pageSize=20", Ct)).StatusCode);
    }

    // 情境：錯誤密碼 → 401 一般訊息（不透露帳號或密碼，FR-003）。
    // @SPEC-004 @AC-3
    [Fact(DisplayName = "錯誤密碼登入回應 401 一般訊息不透露帳密（FR-003）")]
    public async Task Login_wrong_password_returns_401()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/auth/login", new { account = "admin", password = "nope" }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    // 情境：登出（瀏覽器自動帶 refresh Cookie）→ 204 並清除 Cookie；之後同一 access token 存取受保護端點 → 401，
    // 以原 refresh token 換新 → 401（session 已撤銷，FR-005）。
    // @SPEC-004 @AC-5
    [Fact(DisplayName = "登出後 access token 與 refresh token 皆失效回應 401（FR-005）")]
    public async Task Logout_clears_session()
    {
        var client = factory.CreateClient();
        var login = await PostLoginAsync(client, "admin", "Admin123!", Ct);
        var refresh = RefreshTokenOf(login)!;

        var logout = await client.PostAsync("/auth/logout", null, Ct);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=thu, 01 jan 1970", RefreshSetCookie(logout)!.ToLowerInvariant());

        var after = await client.GetAsync("/users", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
        var noCookies = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshWithAsync(noCookies, refresh, Ct)).StatusCode);
    }

    // 情境：未登入（沒有 token 也沒有 refresh Cookie）呼叫登出 → 仍回 204（冪等，FR-005）。
    // @SPEC-004 @AC-5
    [Fact(DisplayName = "未登入呼叫登出仍回應 204（FR-005）")]
    public async Task Logout_without_login_returns_204()
    {
        var res = await factory.CreateClient().PostAsync("/auth/logout", null, Ct);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }
}
