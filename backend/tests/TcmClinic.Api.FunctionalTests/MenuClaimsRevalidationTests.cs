using System.Net;
using System.Net.Http.Json;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 選單可見性 claims 每請求刷新（005 T008，真實 Cookie scheme）：登入後 <c>menu</c> claims 等於其角色
/// 可見 route 之聯集；角色可見性於登入中被變更後，使用者**下次請求**即反映（OnValidatePrincipal 重寫）。
/// 以 <see cref="AuthWebApplicationFactory"/>（不注入 TestAuthHandler）驗證真實重驗路徑。需 Docker Desktop。
/// </summary>
public class MenuClaimsRevalidationTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record Created(Guid Id);
    private sealed record MenuNode(Guid Id, string? Route, MenuNode[] Children);

    // 以種子 admin（具全部三個管理選單可見性）登入。
    private async Task<HttpClient> AdminClientAsync()
    {
        var client = factory.CreateClient();
        var login = await AuthTestSupport.PostLoginAsync(client, "admin", "Admin123!", Ct);
        login.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<Guid> CreateRoleAsync(HttpClient admin, string code)
    {
        var res = await admin.PostAsJsonAsync(
            "/roles", new { code, name = code, description = (string?)null, roleType = "Custom" }, Ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<Created>(Ct))!.Id;
    }

    private async Task<Guid> CreateUserAsync(HttpClient admin, string account, Guid[] roleIds)
    {
        var res = await admin.PostAsJsonAsync(
            "/users", new { account, displayName = account, password = "P@ssw0rd1234", roleIds }, Ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<Created>(Ct))!.Id;
    }

    private async Task<HttpClient> LoginAsAsync(string account)
    {
        var client = factory.CreateClient();
        var login = await AuthTestSupport.PostLoginAsync(client, account, "P@ssw0rd1234", Ct);
        login.EnsureSuccessStatusCode();
        return client;
    }

    // 於選單樹（含子項）中遞迴尋找指定 route 的選單 id。
    private static Guid? FindMenuId(IEnumerable<MenuNode> nodes, string route)
    {
        foreach (var node in nodes)
        {
            if (node.Route == route)
            {
                return node.Id;
            }
            var hit = FindMenuId(node.Children ?? [], route);
            if (hit is not null)
            {
                return hit;
            }
        }
        return null;
    }

    // 情境：角色初始無可見性 → 使用者讀 /users 得 403；admin 於登入中授予該角色 accountMaintenance（帳號維護）可見性後，
    // 使用者**下次請求**即取得存取（200），證明 menu claims 於每請求重驗時依現時 SysRoleTasks 刷新（T008、FR-002）。
    // @SPEC-002 @AC-10
    // @SPEC-005 @AC-10
    [Fact(DisplayName = "角色可見性於登入中變更後使用者下次請求即反映（menu claims 每請求刷新，T008）")]
    public async Task Visibility_change_reflected_in_menu_claims_next_request()
    {
        var admin = await AdminClientAsync();
        var roleId = await CreateRoleAsync(admin, "T008R");
        _ = await CreateUserAsync(admin, "t008u", [roleId]);

        // 初始：角色無任何可見選單 → 使用者讀受控端點回 403。
        var user = await LoginAsAsync("t008u");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await user.GetAsync("/users?page=1&pageSize=20", Ct)).StatusCode);

        // admin 取得種子「帳號維護」選單 id，授予測試角色其可見性。
        var tree = await admin.GetFromJsonAsync<MenuNode[]>("/menus", Ct);
        var usersMenuId = FindMenuId(tree!, "accountMaintenance");
        Assert.NotNull(usersMenuId);

        var rvResp = await admin.GetAsync($"/roles/{roleId}/menu-visibility", Ct);
        var rowVersion = rvResp.Headers.ETag!.Tag.Trim('"');
        var grant = await admin.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = new[] { usersMenuId!.Value }, rowVersion }, Ct);
        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);

        // 下次請求：使用者的 menu claims 已刷新 → 取得 /users 存取（200）。
        Assert.Equal(
            HttpStatusCode.OK,
            (await user.GetAsync("/users?page=1&pageSize=20", Ct)).StatusCode);
    }
}
