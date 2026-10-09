using System.Net;
using System.Net.Http.Json;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>選單端點的功能/契約測試（US1，對真實 SQL 容器）。</summary>
public class MenuEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record MenuNode(Guid Id, string Name, string? Route, bool IsEnabled, MenuNode[] Children);

    private sealed record CreatedMenu(Guid Id);

    // 情境：以超級管理員身分查詢選單樹 → 回 200 OK。
    [Fact(DisplayName = "以超級管理員查詢選單樹應回 200")]
    public async Task Get_menus_returns_ok()
    {
        var response = await _client.GetAsync("/menus", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // 情境：建立選單 → 回 201 Created，且隨後查詢選單樹能看到該項（端到端寫入可見）。
    // @SPEC-002 @AC-1
    // @SPEC-008 @AC-9
    [Fact(DisplayName = "建立選單應回 201 且查詢選單樹能看到該項")]
    public async Task Create_then_appears_in_tree()
    {
        var create = await _client.PostAsJsonAsync(
            "/menus",
            new { name = "選單甲", parentId = (Guid?)null, route = "/a", icon = (string?)null, displayOrder = 0 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var tree = await _client.GetFromJsonAsync<MenuNode[]>(
            "/menus", TestContext.Current.CancellationToken);
        Assert.NotNull(tree);
        Assert.Contains(tree!, n => n.Name == "選單甲");
    }

    // 情境：同一父層建立同名選單兩次 → 第二次回 400 BadRequest（FR-007 同層名稱唯一）。
    // @SPEC-002 @AC-16
    [Fact(DisplayName = "同層建立同名選單第二次應回 400（FR-007 同層名稱唯一）")]
    public async Task Duplicate_name_in_same_level_returns_400()
    {
        var body = new { name = "選單乙", parentId = (Guid?)null, route = "/b", icon = (string?)null, displayOrder = 0 };

        var first = await _client.PostAsJsonAsync("/menus", body, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/menus", body, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    // 情境（FR-020）：指定不存在的父選單 → 回 400（用例先驗證），不讓資料庫外鍵違反變成 500。
    // @SPEC-002 @AC-18
    [Fact(DisplayName = "指定不存在的父選單建立選單應回 400（FR-020）")]
    public async Task Create_with_missing_parent_returns_400()
    {
        var res = await _client.PostAsJsonAsync(
            "/menus",
            new { name = "孤兒選單", parentId = Guid.CreateVersion7(), route = "/orphan", icon = (string?)null, displayOrder = 0 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // 情境（設計決策 R8）：在既有父選單下建立子選單 → 201，且選單樹中出現在該父選單底下（外鍵允許指向既有選單）。
    // @SPEC-002 @AC-3
    [Fact(DisplayName = "在既有父選單下建立子選單應回 201 且出現在父選單底下（設計決策 R8）")]
    public async Task Create_child_under_existing_parent()
    {
        var ct = TestContext.Current.CancellationToken;
        var parent = await _client.PostAsJsonAsync(
            "/menus",
            new { name = "父選單丙", parentId = (Guid?)null, route = (string?)null, icon = (string?)null, displayOrder = 0 }, ct);
        Assert.Equal(HttpStatusCode.Created, parent.StatusCode);
        var parentId = (await parent.Content.ReadFromJsonAsync<CreatedMenu>(ct))!.Id;

        var child = await _client.PostAsJsonAsync(
            "/menus",
            new { name = "子選單丙", parentId, route = "/c", icon = (string?)null, displayOrder = 0 }, ct);
        Assert.Equal(HttpStatusCode.Created, child.StatusCode);

        var tree = await _client.GetFromJsonAsync<MenuNode[]>("/menus", ct);
        var parentNode = Assert.Single(tree!, n => n.Name == "父選單丙");
        Assert.Contains(parentNode.Children, n => n.Name == "子選單丙");
    }
}
