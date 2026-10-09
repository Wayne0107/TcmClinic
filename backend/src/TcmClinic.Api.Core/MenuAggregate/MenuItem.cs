namespace TcmClinic.Api.Core.MenuAggregate;

/// <summary>
/// 選單項目聚合根（對應資料表 SysTask）。以 ParentId 形成階層（null = 頂層；2026-09-30 前以 0 為哨兵，設計決策 R8）。
/// ParentId 為自參考外鍵，父選單必須存在（FR-020，由用例先驗證、資料庫外鍵兜底）。
/// 純容器父項的 Route 為 null（FR-019）。變更方法回傳 this（Principle II）。
/// </summary>
public class MenuItem : AuditableEntityBase<MenuItemId>, IAggregateRoot
{
    /// <summary>對外識別碼（UUIDv7，建構時產生、不可變更；API 一律使用此值，SPEC-064）。</summary>
    public MenuItemPublicId PublicId { get; private set; }

    /// <summary>父選單 Id；null 表示頂層。</summary>
    public MenuItemId? ParentId { get; private set; }
    public MenuItemName Name { get; private set; }
    public string? Route { get; private set; }
    public string? Icon { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    public MenuItem(
        MenuItemName name,
        MenuItemId? parentId,
        string? route = null,
        string? icon = null,
        int displayOrder = 0)
    {
        // 哨兵 0：避免 EF 追蹤時對未初始化 Vogen Id 呼叫 GetHashCode（Principle VII）。
        Id = MenuItemId.From(0);
        PublicId = MenuItemPublicId.New();
        Name = name;
        ParentId = parentId;
        Route = NormalizeRoute(route);
        Icon = icon;
        DisplayOrder = displayOrder;
        IsEnabled = true;
    }

    // EF Core 具現化用。
    private MenuItem()
    {
    }

    /// <summary>純分組容器（無對應前端路由）。</summary>
    public bool IsContainer => Route is null;

    public MenuItem UpdateDetails(MenuItemName name, string? route, string? icon)
    {
        Name = name;
        Route = NormalizeRoute(route);
        Icon = icon;
        return this;
    }

    /// <summary>移到指定父選單之下；傳 null 移到頂層。</summary>
    public MenuItem MoveTo(MenuItemId? parentId)
    {
        ParentId = parentId;
        return this;
    }

    public MenuItem Reorder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        return this;
    }

    public MenuItem SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
        return this;
    }

    private static string? NormalizeRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return null;
        }

        var trimmed = route.Trim();
        Guard.Against.OutOfRange(trimmed.Length, nameof(route), 1, 200);
        return trimmed;
    }
}
