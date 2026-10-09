using System.Net;
using System.Net.Http.Json;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>角色可見選單端點的功能測試（US2，對真實 SQL 容器）。</summary>
public class RoleVisibilityEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record RoleDto(Guid Id, string Code, string Name, string? Description);
    private sealed record CreatedDto(Guid Id);
    private sealed record VisNode(Guid Id, string Name, string? Route, bool Granted, VisNode[] Children);

    private async Task<Guid> SuperAdminIdAsync()
    {
        var roles = await _client.GetFromJsonAsync<RoleDto[]>("/roles", TestContext.Current.CancellationToken);
        return roles!.Single(r => r.Code == "SUPERADMIN").Id;
    }

    private async Task<Guid> CreateRoleAsync(string code, string name)
    {
        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code, name, description = (string?)null, roleType = "Custom" },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedDto>(TestContext.Current.CancellationToken);
        return created!.Id;
    }

    /// <summary>取角色目前並行權杖（由 GET menu-visibility 的 ETag 標頭，去除引號）。012。</summary>
    private async Task<string> RowVersionAsync(Guid roleId)
    {
        var resp = await _client.GetAsync(
            $"/roles/{roleId}/menu-visibility", TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        return resp.Headers.ETag!.Tag.Trim('"');
    }

    private async Task<Guid> CreateMenuAsync(string name, string? route, Guid? parentId = null)
    {
        var response = await _client.PostAsJsonAsync(
            "/menus",
            new { name, parentId, route, icon = (string?)null, displayOrder = 0 },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedDto>(TestContext.Current.CancellationToken);
        return created!.Id;
    }

    // 情境：查詢角色清單 → 應包含種入的 SUPERADMIN 角色（R3 種子）。
    // @SPEC-002 @AC-5
    [Fact(DisplayName = "查詢角色清單應包含種入的 SUPERADMIN 角色（R3 種子）")]
    public async Task Get_roles_includes_seeded_superadmin()
    {
        var roles = await _client.GetFromJsonAsync<RoleDto[]>("/roles", TestContext.Current.CancellationToken);
        Assert.NotNull(roles);
        Assert.Contains(roles!, r => r.Code == "SUPERADMIN");
    }

    // 情境：查詢不存在角色的可見選單 → 回 404 NotFound。
    // @SPEC-002 @AC-5
    // @SPEC-012 @AC-20
    [Fact(DisplayName = "查詢不存在角色的可見選單應回 404")]
    public async Task Get_visibility_for_missing_role_returns_404()
    {
        var response = await _client.GetAsync("/roles/999999/menu-visibility", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // 情境：為角色授權某葉選單並存檔 → 讀回可見性時該葉標記為 Granted（FR-009/010 設定即所得）。
    // @SPEC-002 @AC-6
    // @SPEC-012 @AC-9
    // @SPEC-012 @AC-10
    [Fact(DisplayName = "授權葉選單後讀回可見性該葉應為 Granted（FR-009/010）")]
    public async Task Grant_leaf_is_reflected_in_visibility()
    {
        var roleId = await SuperAdminIdAsync();
        var leafId = await CreateMenuAsync("使用者頁", "/admin/users");
        var rowVersion = await RowVersionAsync(roleId);

        var set = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = new[] { leafId }, rowVersion },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);

        var tree = await _client.GetFromJsonAsync<VisNode[]>(
            $"/roles/{roleId}/menu-visibility", TestContext.Current.CancellationToken);
        Assert.NotNull(tree);
        Assert.Contains(tree!, n => n.Id == leafId && n.Granted);
    }

    // 情境：嘗試把容器父項授權給角色 → 回 400 BadRequest（FR-011 只能授權葉頁面，不能授容器）。
    // 容器＝具子節點者（對齊前端選單），故建立父項並掛一個葉，使父項成為容器。
    // @SPEC-002 @AC-20
    // @SPEC-012 @AC-11
    [Fact(DisplayName = "將容器（具子節點）授權給角色應回 400（FR-011 只能授葉頁面）")]
    public async Task Granting_a_container_returns_400()
    {
        var roleId = await SuperAdminIdAsync();
        var containerId = await CreateMenuAsync("容器選單", null);
        await CreateMenuAsync("容器下的葉", "/admin/leaf-under-container", containerId);
        var rowVersion = await RowVersionAsync(roleId);

        var set = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = new[] { containerId }, rowVersion },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, set.StatusCode);
    }

    // 情境（FR-009，Edge Case）：送出空 grantedMenuItemIds（帶有效 rowVersion）＝清除該角色全部可見選單（合法）。
    // @SPEC-012 @AC-10
    [Fact(DisplayName = "設定權限送出空清單應清除全部授權（012，FR-009）")]
    public async Task Set_empty_visibility_clears_all_grants()
    {
        var roleId = await CreateRoleAsync("VISCLR", "清除測試");
        var leafId = await CreateMenuAsync("頁清除", "/vis/clear");

        var grant = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = new[] { leafId }, rowVersion = await RowVersionAsync(roleId) },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);

        var clear = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = Array.Empty<Guid>(), rowVersion = await RowVersionAsync(roleId) },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);

        var tree = await _client.GetFromJsonAsync<VisNode[]>(
            $"/roles/{roleId}/menu-visibility", TestContext.Current.CancellationToken);
        Assert.NotNull(tree);
        Assert.Contains(Flatten(tree!), n => n.Id == leafId);
        Assert.DoesNotContain(Flatten(tree!), n => n.Granted);
    }

    private static IEnumerable<VisNode> Flatten(IEnumerable<VisNode> nodes) =>
        nodes.SelectMany(n => new[] { n }.Concat(Flatten(n.Children)));

    // 情境（012，US3/FR-015）：設定權限未帶 rowVersion → 回 400（並行權杖必填）。
    // @SPEC-012 @AC-15
    [Fact(DisplayName = "設定權限缺 rowVersion 應回 400（012，FR-015）")]
    public async Task Set_visibility_without_rowversion_returns_400()
    {
        var roleId = await CreateRoleAsync("VIS400", "並行測試A");
        var leafId = await CreateMenuAsync("頁A", "/vis/a");

        var set = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = new[] { leafId } }, // 無 rowVersion
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, set.StatusCode);
    }

    // 情境（012，US3/FR-015/SC-004）：以過期 rowVersion 儲存 → 回 409（他人已先變更，防靜默覆蓋）。
    // @SPEC-012 @AC-15
    [Fact(DisplayName = "設定權限用過期 rowVersion 應回 409（012，SC-004）")]
    public async Task Set_visibility_with_stale_rowversion_returns_409()
    {
        var roleId = await CreateRoleAsync("VIS409", "並行測試B");
        var leafId = await CreateMenuAsync("頁B", "/vis/b");

        var stale = await RowVersionAsync(roleId);

        // 第一次儲存成功，角色 rowVersion 被更新（進版）。
        var first = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = new[] { leafId }, rowVersion = stale },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // 以過期權杖再次儲存 → 409。
        var second = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = Array.Empty<Guid>(), rowVersion = stale },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
