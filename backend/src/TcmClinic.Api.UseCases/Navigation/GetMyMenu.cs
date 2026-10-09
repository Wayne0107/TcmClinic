using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Navigation;

/// <summary>
/// 取得目前使用者的可見選單樹（US3）：依所屬角色（以角色代碼傳入）聯集授權子項，
/// 補上祖先容器、過濾停用，回傳可見選單樹。
/// SPEC-008：回應改為對齊前端之精簡型別 <see cref="MyMenuNodeDto"/>（text/value/subMenu），
/// 僅改變輸出對映，不動可見性/建樹邏輯。
/// </summary>
public sealed record GetMyMenuQuery(IReadOnlyList<string> RoleCodes)
    : IQuery<Result<IReadOnlyList<MyMenuNodeDto>>>;

public sealed class GetMyMenuHandler(
    IReadRepository<Role> roleRepository,
    IReadRepository<MenuItem> menuRepository,
    IReadRepository<RoleMenuVisibility> visibilityRepository)
    : IQueryHandler<GetMyMenuQuery, Result<IReadOnlyList<MyMenuNodeDto>>>
{
    public async ValueTask<Result<IReadOnlyList<MyMenuNodeDto>>> Handle(
        GetMyMenuQuery query, CancellationToken cancellationToken)
    {
        if (query.RoleCodes.Count == 0)
        {
            return Result<IReadOnlyList<MyMenuNodeDto>>.Success([]);
        }

        var roleCodes = query.RoleCodes.ToHashSet();
        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);
        var roleIds = roles
            .Where(r => roleCodes.Contains(r.Code.Value))
            .Select(r => r.Id.Value)
            .ToHashSet();

        if (roleIds.Count == 0)
        {
            return Result<IReadOnlyList<MyMenuNodeDto>>.Success([]);
        }

        var allVisibility = await visibilityRepository.ListAsync(
            new AllRoleMenuVisibilitiesSpec(), cancellationToken);
        IReadOnlySet<int> grantedLeafIds = allVisibility
            .Where(v => roleIds.Contains(v.RoleId.Value))
            .Select(v => v.MenuItemId.Value)
            .ToHashSet();

        var items = await menuRepository.ListAsync(new AllMenuItemsOrderedSpec(), cancellationToken);
        var visible = MenuTree.BuildVisible(items, grantedLeafIds);

        return Result<IReadOnlyList<MyMenuNodeDto>>.Success(Map(visible));
    }

    /// <summary>
    /// 將可見選單節點對映為精簡回應型別：Text←Name、Value←Route、SubMenu←子節點；
    /// 無子節點時 SubMenu 為 null（序列化時省略）。保持既有順序。
    /// </summary>
    private static IReadOnlyList<MyMenuNodeDto> Map(IReadOnlyList<MenuTreeNode> nodes) =>
        nodes.Select(n =>
        {
            var children = Map(n.Children);
            return new MyMenuNodeDto(
                n.Item.Name.Value,
                n.Item.Route,
                children.Count > 0 ? children : null);
        }).ToList();
}
