using TcmClinic.Api.Core.MenuAggregate;

namespace TcmClinic.Api.UnitTests.MenuAggregate;

/// <summary>執行期可見選單解析（US3）：FR-006/012/015、SC-004。</summary>
public class MenuVisibilityTests
{
    private static MenuItem Node(int id, int? parentId, string name, string? route, bool enabled = true)
    {
        var item = new MenuItem(MenuItemName.From(name), parentId is null ? null : MenuItemId.From(parentId.Value), route)
        {
            Id = MenuItemId.From(id),
        };
        if (!enabled)
        {
            item.SetEnabled(false);
        }
        return item;
    }

    // 系統管理(1,容器) > 使用者(2,/u)、角色(3,/r)；首頁(4,/home)
    private static MenuItem[] Sample() =>
    [
        Node(1, null, "系統管理", null),
        Node(2, 1, "使用者", "/u"),
        Node(3, 1, "角色", "/r"),
        Node(4, null, "首頁", "/home"),
    ];

    // 情境：只授權某葉節點 → 其祖先容器應自動顯示、且容器下只含被授權的葉（FR-012 祖先自動顯示）。
    // @SPEC-002 @AC-8
    [Fact(DisplayName = "授權葉節點時祖先容器自動顯示且僅含被授權的葉（FR-012）")]
    public void Granted_leaf_shows_with_ancestor_container()
    {
        var visible = MenuTree.BuildVisible(Sample(), new HashSet<int> { 2 });

        // 只授權「使用者(2)」→ 容器「系統管理」自動顯示、其下只有「使用者」；首頁不顯示。
        Assert.Single(visible);
        Assert.Equal("系統管理", visible[0].Item.Name.Value);
        Assert.Single(visible[0].Children);
        Assert.Equal("使用者", visible[0].Children[0].Item.Name.Value);
    }

    // 情境：只授權某頂層葉 → 未授權子項的容器不顯示（預設拒絕，SC-004 未授權可見率 0%）。
    // @SPEC-002 @AC-8
    [Fact(DisplayName = "未授權子項的容器不顯示（預設拒絕，SC-004）")]
    public void Ungranted_leaves_are_hidden()
    {
        var visible = MenuTree.BuildVisible(Sample(), new HashSet<int> { 4 });

        // 只授權「首頁(4)」→ 只有首頁；系統管理容器不顯示（其下無授權子項）。
        Assert.Single(visible);
        Assert.Equal("首頁", visible[0].Item.Name.Value);
    }

    // 情境：授權多個葉節點 → 可見選單為各授權的聯集（FR-015 多角色/多授權聯集）。
    // @SPEC-002 @AC-9
    [Fact(DisplayName = "多個授權時可見選單為各授權的聯集（FR-015）")]
    public void Multiple_grants_are_union()
    {
        var visible = MenuTree.BuildVisible(Sample(), new HashSet<int> { 2, 3, 4 });

        Assert.Equal(2, visible.Count); // 系統管理 + 首頁
        var admin = visible.Single(n => n.Item.Name.Value == "系統管理");
        Assert.Equal(2, admin.Children.Count); // 使用者 + 角色
    }

    // 情境：被授權但已停用的選單 → 仍應被排除（停用優先於授權，FR-006）。
    // @SPEC-002 @AC-4
    [Fact(DisplayName = "已停用選單即使被授權仍排除（停用優先，FR-006）")]
    public void Disabled_item_is_excluded_even_if_granted()
    {
        var items = new[]
        {
            Node(1, null, "系統管理", null),
            Node(2, 1, "使用者", "/u", enabled: false), // 停用
        };

        var visible = MenuTree.BuildVisible(items, new HashSet<int> { 2 });

        Assert.Empty(visible); // 停用優先於授權（FR-006）
    }

    // 情境：完全沒有授權 → 可見選單為空（預設拒絕）。
    [Fact(DisplayName = "完全沒有授權時可見選單為空（預設拒絕）")]
    public void No_grants_yields_empty()
    {
        Assert.Empty(MenuTree.BuildVisible(Sample(), new HashSet<int>()));
    }
}
