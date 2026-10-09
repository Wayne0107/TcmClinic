namespace TcmClinic.Api.Core.MenuAggregate;

/// <summary>選單樹節點（項目 + 子節點）。</summary>
public sealed record MenuTreeNode(MenuItem Item, IReadOnlyList<MenuTreeNode> Children);

/// <summary>
/// 選單樹建構與階層規則。以扁平清單建樹（ParentId 為 null = 頂層），並提供循環偵測（FR-018）。
/// 註：目前以純函式建模；TreeCollections 保留供後續變更/走訪情境使用（見 設計決策 R1 之偏差說明）。
/// </summary>
public static class MenuTree
{
    /// <summary>由扁平清單建構巢狀選單樹，各層依 DisplayOrder 排序。</summary>
    public static IReadOnlyList<MenuTreeNode> Build(IEnumerable<MenuItem> items)
    {
        var list = items.ToList();

        // 字典鍵不可為 null：頂層另行取出，子項依父選單 Id 分組。
        var byParent = list
            .Where(i => i.ParentId is not null)
            .GroupBy(i => i.ParentId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(i => i.DisplayOrder).ToList());

        return list
            .Where(i => i.ParentId is null)
            .OrderBy(i => i.DisplayOrder)
            .Select(root => new MenuTreeNode(root, BuildLevel(root.Id, byParent)))
            .ToList();
    }

    private static IReadOnlyList<MenuTreeNode> BuildLevel(
        MenuItemId parentId,
        IReadOnlyDictionary<MenuItemId, List<MenuItem>> byParent)
    {
        if (!byParent.TryGetValue(parentId, out var children))
        {
            return [];
        }

        return children
            .Select(child => new MenuTreeNode(child, BuildLevel(child.Id, byParent)))
            .ToList();
    }

    /// <summary>
    /// 依授權建構「可見」選單樹（US3）：先排除停用項（停用優先，FR-006），再保留
    /// 「被授權的具路由子項」及其祖先容器（祖先自動顯示，FR-012）。未授權子項不出現（SC-004）。
    /// <paramref name="grantedLeafIds"/> 為多角色授權的聯集（FR-015）。
    /// </summary>
    public static IReadOnlyList<MenuTreeNode> BuildVisible(
        IEnumerable<MenuItem> items, IReadOnlySet<int> grantedLeafIds)
    {
        var enabled = items.Where(i => i.IsEnabled);
        var full = Build(enabled);
        return Prune(full, grantedLeafIds);
    }

    private static IReadOnlyList<MenuTreeNode> Prune(
        IReadOnlyList<MenuTreeNode> nodes, IReadOnlySet<int> grantedLeafIds)
    {
        var result = new List<MenuTreeNode>();

        foreach (var node in nodes)
        {
            var prunedChildren = Prune(node.Children, grantedLeafIds);
            var isGrantedLeaf = !node.Item.IsContainer && grantedLeafIds.Contains(node.Item.Id.Value);

            if (isGrantedLeaf || prunedChildren.Count > 0)
            {
                result.Add(new MenuTreeNode(node.Item, prunedChildren));
            }
        }

        return result;
    }

    /// <summary>
    /// 將 <paramref name="nodeId"/> 掛到 <paramref name="newParentId"/> 是否會造成循環（FR-018）；
    /// <paramref name="newParentId"/> 為 null 表示移到頂層，不會造成循環。
    /// </summary>
    public static bool WouldCreateCycle(
        IEnumerable<MenuItem> items,
        MenuItemId nodeId,
        MenuItemId? newParentId)
    {
        var byId = items.ToDictionary(i => i.Id);
        var current = newParentId;

        while (current is { } id)
        {
            if (id == nodeId)
            {
                return true;
            }

            if (!byId.TryGetValue(id, out var parent))
            {
                break;
            }

            current = parent.ParentId;
        }

        return false;
    }
}
