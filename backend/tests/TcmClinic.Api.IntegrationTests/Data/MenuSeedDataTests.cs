using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Data;

/// <summary>
/// 開發種子（<see cref="SeedData"/>＋<see cref="MenuSeedData"/>）：業務選單樹以 value 調和既有節點、
/// 不在樹中的舊節點與其可見性軟刪除、可見性只授葉節點且只補不收、SUPERADMIN 可見全部未刪除葉節點（SPEC-009）、重跑冪等。
/// 新增選單時同步更新 <see cref="ExpectedTree"/>。
/// </summary>
public class MenuSeedDataTests : IAsyncLifetime
{
    /// <summary>期望的業務選單樹（深度|名稱|value，依同層順序），與 MenuSeedData.Menu 一致。</summary>
    private const string ExpectedTree =
        """
        0|系統管理|system
        1|角色權限維護|rolePermission
        1|帳號維護|accountMaintenance
        1|個人資料維護|personalData
        """;

    private readonly TestDatabase _db = new();

    private readonly Pbkdf2PasswordHasher _hasher = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_db.GetConnectionString())
            .AddInterceptors(new AuditableEntityInterceptor(new SystemCurrentUser()))
            .Options;
        return new AppDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();
        await using var ctx = NewContext();
        await ctx.Database.MigrateAsync(Ct);
        await SeedData.SeedAsync(ctx, _hasher, "Admin123!", Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task ReseedAsync()
    {
        await using var ctx = NewContext();
        await SeedData.SeedAsync(ctx, _hasher, "Admin123!", Ct);
    }

    /// <summary>業務節點＝Route 非 / 開頭（/admin/* 為後台管理選單）；只取未刪除者。</summary>
    private static async Task<List<MenuItem>> LiveBusinessMenusAsync(AppDbContext ctx) =>
        (await ctx.MenuItems.ToListAsync(Ct)).Where(m => m.Route is not null && !m.Route.StartsWith('/')).ToList();

    /// <summary>將選單攤平成「深度|名稱|value」行（依父子與同層順序），與 <see cref="ExpectedTree"/> 同格式。</summary>
    private static List<string> Flatten(IReadOnlyList<MenuItem> menus)
    {
        var byParent = menus.ToLookup(m => m.ParentId);
        var lines = new List<string>();
        void Walk(MenuItemId? parentId, int depth)
        {
            foreach (var m in byParent[parentId].OrderBy(m => m.DisplayOrder))
            {
                lines.Add($"{depth}|{m.Name.Value}|{m.Route}");
                Walk(m.Id, depth + 1);
            }
        }
        Walk(null, 0);
        return lines;
    }

    private static List<string> Expected() =>
        ExpectedTree.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>某角色未刪除的可見選單之 Route 集合（只計未刪除的可見性與選單）。</summary>
    private static async Task<HashSet<string>> VisibleRoutesAsync(AppDbContext ctx, string roleCode)
    {
        var role = await ctx.Roles.SingleAsync(r => r.Code == RoleCode.From(roleCode), Ct);
        var routeById = (await ctx.MenuItems.ToListAsync(Ct)).ToDictionary(m => m.Id, m => m.Route);

        return (await ctx.RoleMenuVisibilities.ToListAsync(Ct))
            .Where(v => v.RoleId == role.Id && routeById.ContainsKey(v.MenuItemId))
            .Select(v => routeById[v.MenuItemId]!)
            .ToHashSet();
    }

    // 情境：種子後的業務選單樹與 MenuSeedData 定義逐節點一致（名稱、value、同層順序）。
    [Fact(DisplayName = "業務選單樹與種子定義逐節點一致")]
    public async Task Business_menu_tree_matches_definition()
    {
        await using var ctx = NewContext();

        Assert.Equal(Expected(), Flatten(await LiveBusinessMenusAsync(ctx)));
    }

    // 情境：同層順序即陣列索引（0、1、2…），不因節點搬移或刪除而跳號。
    [Fact(DisplayName = "同層顯示順序為連續索引")]
    public async Task Sibling_display_order_is_contiguous()
    {
        await using var ctx = NewContext();
        var menus = await LiveBusinessMenusAsync(ctx);

        foreach (var siblings in menus.GroupBy(m => m.ParentId))
        {
            Assert.Equal(Enumerable.Range(0, siblings.Count()), siblings.Select(m => m.DisplayOrder).OrderBy(o => o));
        }
    }

    // 情境：後台選單管理（/admin/menus，選單管理端點的授權依據）於種子後存在且未刪除。
    [Fact(DisplayName = "後台選單管理 /admin/menus 於種子後存在")]
    public async Task Admin_menu_management_is_seeded()
    {
        await using var ctx = NewContext();

        Assert.Contains("/admin/menus", (await ctx.MenuItems.ToListAsync(Ct)).Select(m => m.Route));
    }

    // 情境（SPEC-009）：SUPERADMIN 可見全部「未刪除」葉節點（含 /admin/menus 與業務功能），容器不入表。
    // @SPEC-009 @AC-1
    // @SPEC-009 @AC-5
    [Fact(DisplayName = "SUPERADMIN 可見葉＝全部未刪除葉節點，容器不入表（SPEC-009）")]
    public async Task Super_admin_sees_all_live_leaves()
    {
        await using var ctx = NewContext();
        var menus = await ctx.MenuItems.ToListAsync(Ct);
        var containerIds = menus.Where(m => m.ParentId is not null).Select(m => m.ParentId!.Value).ToHashSet();
        var expected = menus.Where(m => !containerIds.Contains(m.Id)).Select(m => m.Route!).ToHashSet();

        var routes = await VisibleRoutesAsync(ctx, Role.SuperAdminCode);

        Assert.Equal(expected.Order(), routes.Order());
        Assert.Contains("/admin/menus", routes);
        Assert.Contains("personalData", routes);
        Assert.DoesNotContain(await ctx.RoleMenuVisibilities.ToListAsync(Ct), v => containerIds.Contains(v.MenuItemId));
    }

    // 情境：每個可見葉節點的祖先容器皆存在且未刪除（選單可展開到該葉）。
    // @SPEC-009 @AC-2
    [Fact(DisplayName = "可見葉之祖先容器皆存在且未刪除")]
    public async Task Visible_leaf_ancestors_are_live()
    {
        await using var ctx = NewContext();
        var byId = (await ctx.MenuItems.ToListAsync(Ct)).ToDictionary(m => m.Id);
        var granted = (await ctx.RoleMenuVisibilities.ToListAsync(Ct)).Select(v => v.MenuItemId).Distinct();

        foreach (var leafId in granted)
        {
            Assert.True(byId.TryGetValue(leafId, out var node), "可見性不得指向已刪除選單");
            while (node!.ParentId is { } parentId)
            {
                Assert.True(byId.TryGetValue(parentId, out node), $"祖先 {parentId} 應存在且未刪除");
            }
        }
    }

    // 情境（既有開發庫）：資料庫有不在種子定義中的業務節點且有角色可見，另有節點被移到別的父節點 →
    // 重跑種子後節點回到定義的位置，舊節點與其可見性皆軟刪除。
    [Fact(DisplayName = "既有舊結構經種子調和：節點回到定義位置、舊節點與其可見性軟刪除")]
    public async Task Reseeding_reconciles_legacy_structure()
    {
        await using (var ctx = NewContext())
        {
            var legacyGroup = new MenuItem(MenuItemName.From("舊作業"), null, "legacyGroup", displayOrder: 9);
            ctx.MenuItems.Add(legacyGroup);
            await ctx.SaveChangesAsync(Ct);

            var legacyLeaf = new MenuItem(MenuItemName.From("舊功能"), legacyGroup.Id, "legacyLeaf");
            ctx.MenuItems.Add(legacyLeaf);
            var personalData = await ctx.MenuItems.SingleAsync(m => m.Route == "personalData", Ct);
            personalData.MoveTo(legacyGroup.Id);
            await ctx.SaveChangesAsync(Ct);

            var superAdmin = await ctx.Roles.SingleAsync(r => r.Code == RoleCode.From(Role.SuperAdminCode), Ct);
            ctx.RoleMenuVisibilities.Add(new RoleMenuVisibility(superAdmin.Id, legacyLeaf.Id));
            await ctx.SaveChangesAsync(Ct);
        }

        await ReseedAsync();

        await using var verify = NewContext();
        Assert.Equal(Expected(), Flatten(await LiveBusinessMenusAsync(verify)));

        var all = await verify.MenuItems.IgnoreQueryFilters().ToListAsync(Ct);
        foreach (var legacy in new[] { "legacyGroup", "legacyLeaf" })
        {
            Assert.True(all.Single(m => m.Route == legacy).Removed, $"{legacy} 應已軟刪除");
        }

        var leafId = all.Single(m => m.Route == "legacyLeaf").Id;
        var visibility = await verify.RoleMenuVisibilities.IgnoreQueryFilters().SingleAsync(v => v.MenuItemId == leafId, Ct);
        Assert.True(visibility.Removed, "舊葉節點的可見性應一併軟刪除");
    }

    // 情境：管理者於角色權限維護頁收回的授權（可見性已軟刪除），重跑種子後不會被加回（只補不收）。
    [Fact(DisplayName = "管理者收回的可見性重跑種子後不被加回（只補不收）")]
    public async Task Reseeding_does_not_restore_revoked_visibility()
    {
        await using (var ctx = NewContext())
        {
            var superAdmin = await ctx.Roles.SingleAsync(r => r.Code == RoleCode.From(Role.SuperAdminCode), Ct);
            var menu = await ctx.MenuItems.SingleAsync(m => m.Route == "accountMaintenance", Ct);
            var visibility = await ctx.RoleMenuVisibilities
                .SingleAsync(v => v.RoleId == superAdmin.Id && v.MenuItemId == menu.Id, Ct);
            visibility.Removed = true;
            await ctx.SaveChangesAsync(Ct);
        }

        await ReseedAsync();

        await using var verify = NewContext();
        Assert.DoesNotContain("accountMaintenance", await VisibleRoutesAsync(verify, Role.SuperAdminCode));
    }

    // 情境（SPEC-009 冪等）：重跑種子後角色、選單節點與可見性的列數皆不變。
    // @SPEC-009 @AC-3
    [Fact(DisplayName = "重跑種子後角色、選單與可見性列數不變（冪等）")]
    public async Task Reseeding_is_idempotent()
    {
        static async Task<(int Roles, int Menus, int Visibilities)> CountAsync(AppDbContext ctx) => (
            await ctx.Roles.IgnoreQueryFilters().CountAsync(Ct),
            await ctx.MenuItems.IgnoreQueryFilters().CountAsync(Ct),
            await ctx.RoleMenuVisibilities.IgnoreQueryFilters().CountAsync(Ct));

        (int, int, int) before;
        await using (var ctx = NewContext())
        {
            before = await CountAsync(ctx);
        }

        await ReseedAsync();

        await using var verify = NewContext();
        Assert.Equal(before, await CountAsync(verify));
    }

    // 情境（005 FR-004、US2）：SUPERADMIN 為一般角色、可編輯與刪除，種子說明不得宣稱「不可刪除」誤導管理者。
    // @SPEC-005 @AC-5
    [Fact(DisplayName = "種子建立的 SUPERADMIN 說明不宣稱不可刪除（005，FR-004）")]
    public async Task Super_admin_description_does_not_claim_undeletable()
    {
        await using var ctx = NewContext();
        var superAdmin = await ctx.Roles.IgnoreQueryFilters()
            .SingleAsync(r => r.Code == RoleCode.From(Role.SuperAdminCode), Ct);

        Assert.False(string.IsNullOrWhiteSpace(superAdmin.Description));
        Assert.DoesNotContain("不可刪除", superAdmin.Description);
    }

    // 情境：新建角色未指定種類時，持久化後預設為 Custom（欄位預設 + 領域預設）。
    // @SPEC-012 @AC-4
    [Fact(DisplayName = "新建角色未指定種類時預設為 Custom（012，FR-004）")]
    public async Task New_role_defaults_to_custom()
    {
        await using (var ctx = NewContext())
        {
            ctx.Roles.Add(new Role(RoleCode.From("RT1"), RoleName.From("種類測試")));
            await ctx.SaveChangesAsync(Ct);
        }

        await using var verify = NewContext();
        var role = await verify.Roles.IgnoreQueryFilters().SingleAsync(r => r.Code == RoleCode.From("RT1"), Ct);
        Assert.Equal(RoleType.Custom, role.RoleType);
    }
}
