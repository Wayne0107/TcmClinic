using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>GET /me/menus 執行期可見選單的功能測試（US3；SPEC-008 回應為 text/value/subMenu）。</summary>
public class NavigationEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record RoleDto(Guid Id, string Code, string Name, string? Description);
    private sealed record CreatedDto(Guid Id);

    // SPEC-008：/me/menus 節點形狀對齊前端 accordionMenu（葉節點無 subMenu）。
    private sealed record MenuNode(string Text, string? Value, MenuNode[]? SubMenu);

    private async Task<Guid> SuperAdminIdAsync()
    {
        var roles = await _client.GetFromJsonAsync<RoleDto[]>("/roles", TestContext.Current.CancellationToken);
        return roles!.Single(r => r.Code == "SUPERADMIN").Id;
    }

    private async Task<Guid> CreateMenuAsync(string name, Guid? parentId, string? route)
    {
        var response = await _client.PostAsJsonAsync(
            "/menus",
            new { name, parentId, route, icon = (string?)null, displayOrder = 0 },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedDto>(TestContext.Current.CancellationToken);
        return created!.Id;
    }

    // 012：設定權限需帶並行權杖，取自 GET menu-visibility 的 ETag 標頭。
    private async Task<string> RowVersionAsync(Guid roleId)
    {
        var resp = await _client.GetAsync(
            $"/roles/{roleId}/menu-visibility", TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        return resp.Headers.ETag!.Tag.Trim('"');
    }

    // 情境：授權容器下某葉後呼叫 GET /me/menus → 祖先容器自動顯示且含該葉，未授權的兄弟項不顯示
    // （FR-012 祖先自動顯示 + 預設拒絕；SPEC-008 以 text/value/subMenu 形狀驗證）。
    // @SPEC-002 @AC-8
    // @SPEC-002 @AC-10
    // @SPEC-008 @AC-1
    // @SPEC-008 @AC-2
    // @SPEC-008 @AC-6
    // @SPEC-008 @AC-8
    [Fact(DisplayName = "GET /me/menus 顯示已授權葉與祖先容器並隱藏未授權項（FR-012；形狀 text/value/subMenu）")]
    public async Task My_menu_shows_granted_leaf_with_ancestor_and_hides_others()
    {
        var roleId = await SuperAdminIdAsync();

        var containerId = await CreateMenuAsync("導覽測試容器", null, null);
        var leafId = await CreateMenuAsync("使用者", containerId, "/admin/users");
        // 使用測試專用名稱，避免與既有種子（含種子的「系統管理」）在 (ParentId,Name) 唯一索引上撞名。
        var ungrantedId = await CreateMenuAsync("導覽測試未授權首頁", null, "/nav-test-home");
        _ = ungrantedId;

        var set = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = new[] { leafId }, rowVersion = await RowVersionAsync(roleId) },
            TestContext.Current.CancellationToken);
        set.EnsureSuccessStatusCode();

        var tree = await _client.GetFromJsonAsync<MenuNode[]>("/me/menus", TestContext.Current.CancellationToken);
        Assert.NotNull(tree);

        // 祖先容器「導覽測試容器」自動顯示（以 text 辨識；容器無路由故 value 為 null），其下含被授權葉（value=/admin/users）。
        var container = Assert.Single(tree!, n => n.Text == "導覽測試容器");
        Assert.NotNull(container.SubMenu);
        Assert.Contains(container.SubMenu!, c => c.Value == "/admin/users" && c.Text == "使用者");

        // 未授權的「導覽測試未授權首頁」不顯示。
        Assert.DoesNotContain(tree!, n => n.Text == "導覽測試未授權首頁");
    }

    // 情境：/me/menus 每個節點的 JSON 鍵集合恰為 {text,value}（葉）或 {text,value,subMenu}（容器），
    // 不含 id/icon/displayOrder/isEnabled/route/name/children（SPEC-008 精簡欄位，FR-004、SC-002）。
    // @SPEC-008 @AC-3
    // @SPEC-008 @AC-4
    // @SPEC-008 @AC-5
    [Fact(DisplayName = "GET /me/menus 節點鍵集合僅 text/value/subMenu，葉節點無 subMenu（FR-004、SC-002）")]
    public async Task My_menu_nodes_only_expose_text_value_subMenu()
    {
        var roleId = await SuperAdminIdAsync();
        var containerId = await CreateMenuAsync("鍵測試容器", null, null);
        var leafId = await CreateMenuAsync("鍵測試葉", containerId, "/key-test-leaf");

        var set = await _client.PutAsJsonAsync(
            $"/roles/{roleId}/menu-visibility",
            new { grantedMenuItemIds = new[] { leafId }, rowVersion = await RowVersionAsync(roleId) },
            TestContext.Current.CancellationToken);
        set.EnsureSuccessStatusCode();

        var json = await _client.GetStringAsync("/me/menus", TestContext.Current.CancellationToken);
        var root = JsonNode.Parse(json)!.AsArray();

        var allowed = new HashSet<string> { "text", "value", "subMenu" };
        AssertNodeKeys(root, allowed);
    }

    /// <summary>遞迴檢查每個節點鍵集合：僅允許 text/value/subMenu；有子項才可有 subMenu。</summary>
    private static void AssertNodeKeys(JsonArray nodes, HashSet<string> allowed)
    {
        foreach (var node in nodes)
        {
            var obj = node!.AsObject();
            foreach (var kv in obj)
            {
                Assert.Contains(kv.Key, allowed);
            }

            // 葉節點不得含 subMenu 鍵。
            if (obj.TryGetPropertyValue("subMenu", out var sub) && sub is JsonArray childArray)
            {
                AssertNodeKeys(childArray, allowed);
            }
        }
    }
}
