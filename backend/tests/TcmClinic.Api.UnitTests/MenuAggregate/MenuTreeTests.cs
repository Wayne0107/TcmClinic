using TcmClinic.Api.Core.MenuAggregate;

namespace TcmClinic.Api.UnitTests.MenuAggregate;

public class MenuTreeTests
{
    private static MenuItem Node(int id, int? parentId, string name, int order = 0)
    {
        var item = new MenuItem(MenuItemName.From(name), parentId is null ? null : MenuItemId.From(parentId.Value), $"/r/{id}")
        {
            Id = MenuItemId.From(id),
        };
        item.Reorder(order);
        return item;
    }

    // 情境：混序的扁平清單建樹 → 頂層與各層子項皆依 DisplayOrder 排序、正確巢狀（FR-005 排序）。
    // @SPEC-002 @AC-3
    [Fact(DisplayName = "扁平清單建樹時各層皆依 DisplayOrder 排序並正確巢狀（FR-005）")]
    public void Build_nests_children_under_parents_ordered()
    {
        var items = new[]
        {
            Node(1, null, "系統管理", order: 10),
            Node(3, 1, "角色", order: 20),
            Node(2, 1, "使用者", order: 10),
            Node(4, null, "首頁", order: 5),
        };

        var tree = MenuTree.Build(items);

        // 頂層依 DisplayOrder：首頁(5) 在 系統管理(10) 之前。
        Assert.Equal(2, tree.Count);
        Assert.Equal("首頁", tree[0].Item.Name.Value);
        Assert.Equal("系統管理", tree[1].Item.Name.Value);

        // 系統管理的子項依序：使用者(10)、角色(20)。
        var admin = tree[1];
        Assert.Equal(2, admin.Children.Count);
        Assert.Equal("使用者", admin.Children[0].Item.Name.Value);
        Assert.Equal("角色", admin.Children[1].Item.Name.Value);
    }

    // 情境：把節點掛到自己或其子孫 → 應偵測為循環並阻擋；掛到頂層則允許（FR-018 循環阻擋）。
    // @SPEC-002 @AC-17
    [Fact(DisplayName = "掛到自己或子孫偵測為循環並阻擋、掛到頂層則允許（FR-018）")]
    public void WouldCreateCycle_detects_self_and_descendant_parenting()
    {
        var items = new[]
        {
            Node(1, null, "A"),
            Node(2, 1, "B"),
            Node(3, 2, "C"),
        };

        // 將 A(1) 掛到自己 → 循環。
        Assert.True(MenuTree.WouldCreateCycle(items, MenuItemId.From(1), MenuItemId.From(1)));

        // 將 A(1) 掛到其子孫 C(3) → 循環。
        Assert.True(MenuTree.WouldCreateCycle(items, MenuItemId.From(1), MenuItemId.From(3)));

        // 將 C(3) 掛到頂層 → 不循環。
        Assert.False(MenuTree.WouldCreateCycle(items, MenuItemId.From(3), null));
    }
}
