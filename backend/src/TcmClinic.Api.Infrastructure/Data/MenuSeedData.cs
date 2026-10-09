using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;

namespace TcmClinic.Api.Infrastructure.Data;

/// <summary>
/// 冪等種入業務角色、選單樹與角色可見選單（開發環境種子）。
/// 選單以後端為權威來源、內容以 specs/ 規格清單為準：新增功能時，隨其規格把選單節點加入 <see cref="Menu"/>，
/// 初始授權加入 <see cref="InitialGrants"/>。與後台管理選單（<c>/admin/*</c>，由 <see cref="SeedData"/> 種入）共存、不動。
/// </summary>
public static class MenuSeedData
{
    /*
     * [種子決策摘要]
     * 1. 每個節點（含容器）的前端 value 一律存入 MenuItem.Route（不新增欄位、不改 schema）。
     * 2. 容器/功能項以「是否有子節點」判定（有子節點＝容器）；不以 Route is null 判定。
     * 3. 單一選單樹，以 value（＝Route）為鍵調和既有節點：不存在則新增，存在則依新樹更新名稱、父節點與同層順序。
     * 4. 業務節點＝Route 非 "/" 開頭者（"/admin/*" 為後台管理選單，不在調和範圍）；不在新樹中的業務節點軟刪除，
     *    其可見性一併軟刪除。先刪後調和：唯一索引 (ParentId, Name) 篩選 Removed = 0，避免搬移時與舊節點同名衝突。
     * 5. 角色可見性只種葉節點；初始只授予已實作功能所需（InitialGrants），只補不收——不收回管理者於角色權限維護頁的設定。
     * 6. 種子整體僅於 Development 執行（Program.cs 的 IsDevelopment 守門）。
     */

    /// <summary>來源節點（對應前端選單的 text／value／subMenu）。</summary>
    private sealed record Node(string Text, string Value, Node[]? Children = null);

    /// <summary>建立葉節點（前端無 subMenu 的可點擊功能項）。</summary>
    private static Node Leaf(string text, string value) => new(text, value);

    /// <summary>建立容器節點（前端有 subMenu 的分組項）。</summary>
    private static Node Group(string text, string value, params Node[] children) => new(text, value, children);

    /// <summary>業務角色（代碼、名稱）。SUPERADMIN 由 <see cref="SeedData"/> 種入，不列在此。新增角色時加在這裡。</summary>
    private static readonly (string Code, string Name)[] BusinessRoles = [];

    /// <summary>
    /// 單一選單樹：同層順序即陣列索引。新增功能時隨其規格加入本樹。
    /// value 即端點授權鍵（<c>Policies("MenuAccess:&lt;value&gt;")</c>）。
    /// </summary>
    private static readonly Node[] Menu =
    [
        Group("系統管理", "system",
            Leaf("角色權限維護", "rolePermission"),
            Leaf("帳號維護", "accountMaintenance"),
            Leaf("個人資料維護", "personalData")),
    ];

    /// <summary>
    /// 業務角色的初始可見性：只授予已實作功能所需，其餘由管理者於角色權限維護頁設定。
    /// 例：<c>("personalData", ["CLERK", "MANAGER"])</c>。SUPERADMIN 於開發環境自動可見全部葉節點（SPEC-009），不必列在此。
    /// </summary>
    private static readonly (string Route, string[] RoleCodes)[] InitialGrants = [];

    /// <summary>
    /// 冪等種入業務角色、選單樹與角色可見選單；重跑不產生重複列，既有開發庫依新樹調和。
    /// </summary>
    /// <param name="context">應用程式資料庫內容（含稽核攔截器）。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var roleIdByCode = await SeedRolesAsync(context, cancellationToken);
        await RemoveLegacyMenusAsync(context, cancellationToken);
        var menuIdByValue = await ReconcileMenuTreeAsync(context, cancellationToken);
        await SeedVisibilityAsync(context, roleIdByCode, menuIdByValue, cancellationToken);
    }

    /// <summary>種入業務角色（以 RoleCode 判存在），回傳 code→RoleId 對照（含既有角色，如 SUPERADMIN）。</summary>
    private static async Task<Dictionary<string, RoleId>> SeedRolesAsync(
        AppDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.Roles.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var idByCode = existing.ToDictionary(r => r.Code.Value, r => r.Id);

        var added = false;
        foreach (var (code, name) in BusinessRoles)
        {
            if (idByCode.ContainsKey(code))
            {
                continue;
            }

            context.Roles.Add(new Role(RoleCode.From(code), RoleName.From(name)));
            added = true;
        }

        if (added)
        {
            await context.SaveChangesAsync(cancellationToken);
            // 重新載入以取得新指派的 Id。
            idByCode = (await context.Roles.IgnoreQueryFilters().ToListAsync(cancellationToken))
                .ToDictionary(r => r.Code.Value, r => r.Id);
        }

        return idByCode;
    }

    /// <summary>業務節點：Route 有值且非 "/" 開頭（"/admin/*" 為後台管理選單）。</summary>
    private static bool IsBusinessNode(MenuItem item) => item.Route is not null && !item.Route.StartsWith('/');

    /// <summary>
    /// 不在新樹中的業務舊節點與其可見性軟刪除。直接設定 Removed（等同攔截器的軟刪除轉換，稽核欄與異動紀錄照常產生），
    /// 不走 EF 實體刪除——父子自參考外鍵為 Restrict，實體刪除仍有子節點的父節點會被 EF 擋下。
    /// </summary>
    private static async Task RemoveLegacyMenusAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var keep = Flatten(Menu).Select(n => n.Node.Value).ToHashSet();
        var legacy = (await context.MenuItems.ToListAsync(cancellationToken))
            .Where(m => IsBusinessNode(m) && !keep.Contains(m.Route!))
            .ToList();
        if (legacy.Count == 0)
        {
            return;
        }

        var legacyIds = legacy.Select(m => m.Id).ToHashSet();
        foreach (var visibility in (await context.RoleMenuVisibilities.ToListAsync(cancellationToken))
                     .Where(v => legacyIds.Contains(v.MenuItemId)))
        {
            visibility.Removed = true;
        }

        foreach (var item in legacy)
        {
            item.Removed = true;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 依單一選單樹調和未刪除的業務節點——不存在則新增，存在則更新名稱、父節點與同層順序。
    /// 回傳 value→MenuItemId 對照。
    /// </summary>
    private static async Task<Dictionary<string, MenuItemId>> ReconcileMenuTreeAsync(
        AppDbContext context, CancellationToken cancellationToken)
    {
        var byValue = new Dictionary<string, MenuItem>();
        foreach (var item in (await context.MenuItems.ToListAsync(cancellationToken)).Where(IsBusinessNode))
        {
            byValue.TryAdd(item.Route!, item);
        }

        foreach (var (node, parentValue, order) in Flatten(Menu))
        {
            MenuItemId? parentId = parentValue is null ? null : byValue[parentValue].Id;
            var name = MenuItemName.From(node.Text);

            if (byValue.TryGetValue(node.Value, out var existing))
            {
                if (existing.Name != name)
                {
                    existing.UpdateDetails(name, existing.Route, existing.Icon);
                }

                if (existing.ParentId != parentId)
                {
                    existing.MoveTo(parentId);
                }

                if (existing.DisplayOrder != order)
                {
                    existing.Reorder(order);
                }

                continue;
            }

            var created = new MenuItem(name, parentId, route: node.Value, displayOrder: order);
            context.MenuItems.Add(created);
            await context.SaveChangesAsync(cancellationToken); // 需落庫取得 Id 供子節點引用
            byValue[node.Value] = created;
        }

        await context.SaveChangesAsync(cancellationToken);
        return byValue.ToDictionary(kv => kv.Key, kv => kv.Value.Id);
    }

    /// <summary>
    /// 業務角色依 <see cref="InitialGrants"/> 補上初始可見性，SUPERADMIN 補上全部未刪除葉節點；只補不收。
    /// 以 (RoleId, MenuItemId) 判存在——含已軟刪除的列，管理者於角色權限維護頁移除的授權不會被種子加回。
    /// </summary>
    private static async Task SeedVisibilityAsync(
        AppDbContext context,
        Dictionary<string, RoleId> roleIdByCode,
        Dictionary<string, MenuItemId> menuIdByValue,
        CancellationToken cancellationToken)
    {
        var granted = (await context.RoleMenuVisibilities.IgnoreQueryFilters().ToListAsync(cancellationToken))
            .Select(v => (v.RoleId, v.MenuItemId))
            .ToHashSet();
        var added = false;

        void Grant(RoleId roleId, MenuItemId menuId)
        {
            if (granted.Add((roleId, menuId)))
            {
                context.RoleMenuVisibilities.Add(new RoleMenuVisibility(roleId, menuId));
                added = true;
            }
        }

        foreach (var (route, roleCodes) in InitialGrants)
        {
            foreach (var roleCode in roleCodes)
            {
                Grant(roleIdByCode[roleCode], menuIdByValue[route]);
            }
        }

        /*
         * [開發模式便利 SPEC-009]
         * SUPERADMIN（系統內建超管，開發模式登入帳號 admin，UserName「系統管理員」）於開發環境
         * 刻意可見「全部選單」——即所有未刪除選單的葉節點（含後台管理選單 /admin/menus 與全部業務功能項）——
         * 省去開發／驗證時逐一切換角色帳號才能檢視各功能頁的成本。只取未刪除選單，不授予已軟刪除的舊節點。
         * 只授葉節點（容器可展開性由 ParentId 鏈保證）；種子整體僅於 Development 執行，正式環境不落地此擴權。
         */
        if (roleIdByCode.TryGetValue(Role.SuperAdminCode, out var superAdminId))
        {
            var menus = await context.MenuItems.ToListAsync(cancellationToken);
            var containerIds = menus.Where(m => m.ParentId is not null).Select(m => m.ParentId!.Value).ToHashSet();
            foreach (var leaf in menus.Where(m => !containerIds.Contains(m.Id)))
            {
                Grant(superAdminId, leaf.Id);
            }
        }

        if (added)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>先序走訪（父先於子），回傳每個節點、其父節點 value（頂層為 null）與同層順序。</summary>
    private static IEnumerable<(Node Node, string? ParentValue, int Order)> Flatten(
        IReadOnlyList<Node> nodes, string? parentValue = null)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            yield return (node, parentValue, i);
            if (node.Children is not null)
            {
                foreach (var child in Flatten(node.Children, node.Value))
                {
                    yield return child;
                }
            }
        }
    }
}
