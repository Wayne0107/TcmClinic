using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.Infrastructure.Data;

/// <summary>
/// 種入受保護的超級管理員角色（RoleCode='SUPERADMIN'）與一個初始超管使用者，
/// 確保系統首次可被登入與管理（R6、FR-017）。冪等：已存在則略過。
/// </summary>
public static class SeedData
{
    /// <summary>預設初始超管帳號（正規化為小寫）。</summary>
    public const string DefaultAdminAccount = "admin";

    public static async Task SeedAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        string initialAdminPassword,
        CancellationToken cancellationToken = default)
    {
        var superAdmin = await context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Code == RoleCode.From(Role.SuperAdminCode), cancellationToken);

        if (superAdmin is null)
        {
            superAdmin = new Role(
                RoleCode.From(Role.SuperAdminCode),
                RoleName.From("超級管理員"),
                // 005 FR-004：一般角色（可編輯、可刪除），權限完全由選單可見性決定；僅因種子而初始具管理選單可見性。
                "初始管理角色（一般角色，可編輯與刪除；權限依選單可見性）");

            context.Roles.Add(superAdmin);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 內建管理選單 + 授予種子管理角色可見性：使 admin 登入後即見選單並可操作
        // （005 T031，調整 clarify Q2「不使用種子資料」為「種子提供 admin 初始可見性」）。
        await SeedAdminMenusAsync(context, superAdmin.Id, cancellationToken);

        var adminAccount = UserAccount.From(DefaultAdminAccount);
        var adminExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Account == adminAccount, cancellationToken);

        if (!adminExists)
        {
            var (hash, salt) = passwordHasher.Hash(initialAdminPassword);
            var admin = new User(adminAccount, UserName.From("系統管理員"))
                .SetPassword(hash, salt, DateTime.Now)
                .ReplaceRoles([superAdmin.Id]);

            context.Users.Add(admin);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 於既有種子完成後，附加種入業務角色／選單樹／角色可見性（MenuSeedData，冪等、共存）。
        await MenuSeedData.SeedAsync(context, cancellationToken);
    }

    /// <summary>
    /// 冪等種入內建管理選單（選單管理）並授予指定角色其可見性，使該角色使用者登入後即見選單並可操作（005 授權來源）。
    /// </summary>
    private static async Task SeedAdminMenusAsync(
        AppDbContext context, RoleId roleId, CancellationToken cancellationToken)
    {
        (string Name, string Route)[] pages =
        [
            ("選單管理", "/admin/menus"),
        ];

        var existing = await context.MenuItems.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var newlyAdded = false;
        foreach (var (name, route) in pages)
        {
            if (existing.All(m => m.Route != route))
            {
                context.MenuItems.Add(new MenuItem(MenuItemName.From(name), parentId: null, route));
                newlyAdded = true;
            }
        }
        if (newlyAdded)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        var routeSet = pages.Select(p => p.Route).ToHashSet();
        var targetMenuIds = (await context.MenuItems.IgnoreQueryFilters().ToListAsync(cancellationToken))
            .Where(m => m.Route is not null && routeSet.Contains(m.Route))
            .Select(m => m.Id)
            .ToList();
        var granted = (await context.RoleMenuVisibilities.IgnoreQueryFilters().ToListAsync(cancellationToken))
            .Where(v => v.RoleId == roleId)
            .Select(v => v.MenuItemId)
            .ToHashSet();
        var grantedAdded = false;
        foreach (var menuId in targetMenuIds)
        {
            if (!granted.Contains(menuId))
            {
                context.RoleMenuVisibilities.Add(new RoleMenuVisibility(roleId, menuId));
                grantedAdded = true;
            }
        }
        if (grantedAdded)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
