using Ardalis.Specification;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Auth;
using NSubstitute;

namespace TcmClinic.Api.UnitTests.Authorization;

/// <summary>
/// GetVisibleMenuRoutes 查詢（005 授權來源，T002）：依角色代碼聚合可見「具路由選單」之 route——
/// 多角色聯集、排除容器（Route=null）、無/未知角色回空。
/// </summary>
public class GetVisibleMenuRoutesTests
{
    private static Role Role(int id, string code) =>
        new(RoleCode.From(code), RoleName.From(code)) { Id = RoleId.From(id) };

    private static MenuItem Menu(int id, string? route) =>
        new(MenuItemName.From($"m{id}"), null, route) { Id = MenuItemId.From(id) };

    private static RoleMenuVisibility Grant(int roleId, int menuId) =>
        new(RoleId.From(roleId), MenuItemId.From(menuId));

    // 以 NSubstitute 假造三個唯讀 repository，回傳指定的角色/選單/可見性清單。
    private static GetVisibleMenuRoutesHandler HandlerWith(
        IReadOnlyList<Role> roles,
        IReadOnlyList<MenuItem> menus,
        IReadOnlyList<RoleMenuVisibility> grants)
    {
        var roleRepo = Substitute.For<IReadRepository<Role>>();
        roleRepo.ListAsync(Arg.Any<ISpecification<Role>>(), Arg.Any<CancellationToken>())
            .Returns(roles.ToList());

        var menuRepo = Substitute.For<IReadRepository<MenuItem>>();
        menuRepo.ListAsync(Arg.Any<ISpecification<MenuItem>>(), Arg.Any<CancellationToken>())
            .Returns(menus.ToList());

        var visRepo = Substitute.For<IReadRepository<RoleMenuVisibility>>();
        visRepo.ListAsync(Arg.Any<ISpecification<RoleMenuVisibility>>(), Arg.Any<CancellationToken>())
            .Returns(grants.ToList());

        return new GetVisibleMenuRoutesHandler(roleRepo, menuRepo, visRepo);
    }

    // 情境：空角色代碼 → 直接回空集合（不查資料，預設拒絕）。
    [Fact(DisplayName = "空角色代碼時回傳空集合（預設拒絕）")]
    public async Task Empty_role_codes_returns_empty()
    {
        var handler = HandlerWith([], [], []);
        var result = await handler.Handle(new GetVisibleMenuRoutesQuery([]), CancellationToken.None);
        Assert.Empty(result);
    }

    // 情境：角色代碼查無對應角色 → 回空集合。
    [Fact(DisplayName = "角色代碼查無對應角色時回傳空集合")]
    public async Task Unknown_role_code_returns_empty()
    {
        var handler = HandlerWith([Role(1, "A")], [Menu(10, "/a")], [Grant(1, 10)]);
        var result = await handler.Handle(new GetVisibleMenuRoutesQuery(["NOPE"]), CancellationToken.None);
        Assert.Empty(result);
    }

    // 情境：多角色可見 route 為聯集，且排除容器（Route=null）與未授權葉（FR-002）。
    // @SPEC-002 @AC-9
    // @SPEC-004 @AC-4
    // @SPEC-005 @AC-3
    [Fact(DisplayName = "多角色可見 route 為聯集、排除容器與未授權葉（FR-002）")]
    public async Task Union_excludes_container_and_ungranted()
    {
        var roles = new[] { Role(1, "A"), Role(2, "B") };
        var menus = new[]
        {
            Menu(100, null),      // 容器（無 route）——即使被授權也應排除
            Menu(101, "/users"),  // 角色 A 授權
            Menu(102, "/roles"),  // 角色 B 授權
            Menu(103, "/menus"),  // 無人授權
        };
        var grants = new[] { Grant(1, 100), Grant(1, 101), Grant(2, 102) };

        var handler = HandlerWith(roles, menus, grants);
        var result = await handler.Handle(
            new GetVisibleMenuRoutesQuery(["A", "B"]), CancellationToken.None);

        Assert.Equal(
            new[] { "/roles", "/users" },
            result.OrderBy(r => r, StringComparer.Ordinal).ToArray());
    }

    // 情境：單一角色僅取其自身被授權且具路由的葉（不含他角色授權）。
    [Fact(DisplayName = "單一角色僅取自身被授權且具路由的葉")]
    public async Task Single_role_takes_only_its_granted_routes()
    {
        var roles = new[] { Role(1, "A"), Role(2, "B") };
        var menus = new[] { Menu(101, "/users"), Menu(102, "/roles") };
        var grants = new[] { Grant(1, 101), Grant(2, 102) };

        var handler = HandlerWith(roles, menus, grants);
        var result = await handler.Handle(
            new GetVisibleMenuRoutesQuery(["A"]), CancellationToken.None);

        Assert.Equal(new[] { "/users" }, result.ToArray());
    }
}
