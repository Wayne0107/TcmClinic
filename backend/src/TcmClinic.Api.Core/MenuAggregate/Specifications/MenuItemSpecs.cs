using Ardalis.Specification;

namespace TcmClinic.Api.Core.MenuAggregate.Specifications;

/// <summary>依父項列出選單（<c>null</c> 為頂層），依顯示順序排序。</summary>
public sealed class MenuItemsByParentSpec : Specification<MenuItem>
{
    public MenuItemsByParentSpec(MenuItemId? parentId)
    {
        Query
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.DisplayOrder);
    }
}

/// <summary>取得全部選單（未刪除，由全域過濾器處理），依父項與顯示順序排序，供建樹。</summary>
public sealed class AllMenuItemsOrderedSpec : Specification<MenuItem>
{
    public AllMenuItemsOrderedSpec()
    {
        Query
            .OrderBy(m => m.ParentId)
            .ThenBy(m => m.DisplayOrder)
            .AsNoTracking()
            .TagWith(nameof(AllMenuItemsOrderedSpec));
    }
}

/// <summary>同一父層下、指定名稱的選單（供同層唯一性檢查，FR-007）。</summary>
public sealed class MenuItemByParentAndNameSpec : Specification<MenuItem>
{
    public MenuItemByParentAndNameSpec(MenuItemId? parentId, MenuItemName name)
    {
        Query.Where(m => m.ParentId == parentId && m.Name == name);
    }
}

/// <summary>依對外識別碼（PublicId，SPEC-064）取單一選單。</summary>
public sealed class MenuItemByPublicIdSpec : Specification<MenuItem>
{
    public MenuItemByPublicIdSpec(MenuItemPublicId publicId)
    {
        Query.Where(m => m.PublicId == publicId);
    }
}
