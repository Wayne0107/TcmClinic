using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Data;

/// <summary>
/// 批次新增回歸測試：同一 SaveChanges 內新增多筆時，每筆須取得相異 Id，不可觸發 EF「same key value is already
/// being tracked」衝突（2026-10 以前程式端 Max+1 配號曾在「儲存可見選單」發生 500；現由資料庫 IDENTITY 配號，ADR-010）。
/// </summary>
public class IdentityBatchInsertTests : IAsyncLifetime
{
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

    // 情境：一次授權多個選單給同一角色（同批新增多筆 RoleMenuVisibility）→ 各取得相異 Id、不衝突。
    [Fact(DisplayName = "同批新增多筆角色選單可見性時各自取得相異 Id 不衝突")]
    public async Task Batch_role_menu_visibility_assigns_distinct_ids()
    {
        await using var ctx = NewContext();

        var superAdmin = await ctx.Roles
            .IgnoreQueryFilters()
            .FirstAsync(r => r.Code == RoleCode.From(Role.SuperAdminCode), Ct);

        var menus = new[]
        {
            new MenuItem(MenuItemName.From("VG選單A"), null, "/vg/a"),
            new MenuItem(MenuItemName.From("VG選單B"), null, "/vg/b"),
            new MenuItem(MenuItemName.From("VG選單C"), null, "/vg/c"),
        };
        ctx.MenuItems.AddRange(menus);
        await ctx.SaveChangesAsync(Ct);

        // 一次授權三個選單（重現「儲存可見選單」的批次新增；修正前此處會擲重複 Id 追蹤衝突）。
        var visibilities = menus
            .Select(m => new RoleMenuVisibility(superAdmin.Id, m.Id))
            .ToList();
        ctx.RoleMenuVisibilities.AddRange(visibilities);
        await ctx.SaveChangesAsync(Ct);

        var ids = visibilities.Select(v => v.Id.Value).ToList();
        Assert.Equal(3, ids.Count);
        Assert.Equal(3, ids.Distinct().Count());
    }
}
