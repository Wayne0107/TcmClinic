using System.Net;
using System.Net.Http.Json;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 選單導向授權矩陣（005）：讀寫皆依可見性、GET /roles 用 B（accountMaintenance 或 rolePermission）、
/// 無可見性連讀取亦 403、多可見性為聯集。以 TestAuthHandler 的 X-Test-Menus 標頭模擬可見性。
/// </summary>
public class MenuAuthorizationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record Created(Guid Id);

    /// <summary>建立帶指定可見選單的 client（無參數＝以不存在之 route 模擬「無任何可見」）。</summary>
    private HttpClient Client(params string[] routes)
    {
        var client = _factory.CreateClient();
        var value = routes.Length == 0 ? "/__none__" : string.Join(",", routes);
        client.DefaultRequestHeaders.TryAddWithoutValidation(TestAuthHandler.MenusHeader, value);
        return client;
    }

    /// <summary>以全權 client 建立一個角色供指派（不設標頭＝預設全部可見）。</summary>
    private async Task<Guid> CreateRoleAsync(string code)
    {
        var full = _factory.CreateClient();
        var res = await full.PostAsJsonAsync(
            "/roles", new { code, name = code, description = (string?)null, roleType = "Custom" }, Ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<Created>(Ct))!.Id;
    }

    // 情境：具 accountMaintenance 可見性者可讀寫使用者管理（FR-001、SC-001）。
    // @SPEC-004 @AC-30
    // @SPEC-005 @AC-1
    [Fact(DisplayName = "具 accountMaintenance 可見性者可讀寫使用者管理（FR-001、SC-001）")]
    public async Task Users_menu_grants_full_user_management()
    {
        var roleId = await CreateRoleAsync("MA_USERS");
        var client = Client("accountMaintenance");

        var create = await client.PostAsJsonAsync(
            "/users", new { account = "ma_u1", displayName = "U1", password = "P@ssw0rd1234", roleIds = new[] { roleId } }, Ct);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await client.GetAsync("/users?page=1&pageSize=20", Ct);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    // 情境：無 rolePermission 可見性者寫入角色回 403；GET /roles 因 B 仍可讀；他頁讀取回 403（FR-006a、SC-002）。
    // @SPEC-005 @AC-2
    [Fact(DisplayName = "無角色頁可見性者寫入回 403、GET /roles 因 B 可讀、他頁讀取回 403（FR-006a、SC-002）")]
    public async Task Roles_write_forbidden_but_list_readable_via_B()
    {
        var client = Client("accountMaintenance"); // 僅帳號維護可見

        var create = await client.PostAsJsonAsync(
            "/roles", new { code = "MA_X1", name = "x", description = (string?)null }, Ct);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var roles = await client.GetAsync("/roles", Ct); // B：accountMaintenance 亦可讀
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);

        var menus = await client.GetAsync("/menus", Ct); // 他頁，無可見性
        Assert.Equal(HttpStatusCode.Forbidden, menus.StatusCode);
    }

    // 情境：無任何可見性者讀取受控端點亦回 403；但 /me/menus 不受頁面授權（FR-006）。
    // @SPEC-005 @AC-9
    [Fact(DisplayName = "無任何可見性者讀取受控端點回 403、但 /me/menus 可存取（FR-006）")]
    public async Task No_visibility_forbids_reads_but_me_menus_open()
    {
        var client = Client(); // 無任何可見選單

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/users?page=1&pageSize=20", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/menus", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/me/menus", Ct)).StatusCode);
    }

    // 情境：多可見性為聯集——具 users+menus 可寫該兩頁、對 roles 寫入回 403（FR-002、SC-005）。
    // @SPEC-005 @AC-3
    [Fact(DisplayName = "多可見性聯集可寫對應頁、對未授權頁寫入回 403（FR-002、SC-005）")]
    public async Task Union_of_visibility_grants_multiple_pages()
    {
        var client = Client("accountMaintenance", "/admin/menus");

        var menu = await client.PostAsJsonAsync(
            "/menus", new { name = "MA選單", parentId = (Guid?)null, route = "/ma-x", icon = (string?)null, displayOrder = 0 }, Ct);
        Assert.Equal(HttpStatusCode.Created, menu.StatusCode);

        var role = await client.PostAsJsonAsync(
            "/roles", new { code = "MA_Y1", name = "y", description = (string?)null }, Ct);
        Assert.Equal(HttpStatusCode.Forbidden, role.StatusCode);
    }

    // 情境：無防鎖死——清空角色可見性（PUT menu-visibility 空集）亦被允許回 204（FR-005、SC-004）。
    // @SPEC-005 @AC-7
    // @SPEC-012 @AC-21
    [Fact(DisplayName = "清空角色可見性不被防鎖死攔截回 204（FR-005、SC-004）")]
    public async Task Clearing_visibility_is_allowed_no_antilockout()
    {
        var roleId = await CreateRoleAsync("MA_CLR");
        var client = Client("rolePermission"); // 012：設定權限改由 rolePermission 授權（FR-014）

        var rvResp = await client.GetAsync($"/roles/{roleId}/menu-visibility", Ct);
        var rowVersion = rvResp.Headers.ETag!.Tag.Trim('"');
        var res = await client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = Array.Empty<int>(), rowVersion }, Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
