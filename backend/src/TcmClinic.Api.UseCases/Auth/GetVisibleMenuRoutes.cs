using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Auth;

/// <summary>
/// 依角色代碼回傳其可見之「具路由選單」的 route 集合（005 授權來源）。多角色為聯集、排除容器
/// （`Route=null`）。與 <c>GetMyMenu</c> 同源之可見性聚合，惟只取 route 供授權判定。
/// </summary>
public sealed record GetVisibleMenuRoutesQuery(IReadOnlyList<string> RoleCodes)
    : IQuery<IReadOnlySet<string>>;

public sealed class GetVisibleMenuRoutesHandler(
    IReadRepository<Role> roleRepository,
    IReadRepository<MenuItem> menuRepository,
    IReadRepository<RoleMenuVisibility> visibilityRepository)
    : IQueryHandler<GetVisibleMenuRoutesQuery, IReadOnlySet<string>>
{
    public async ValueTask<IReadOnlySet<string>> Handle(
        GetVisibleMenuRoutesQuery query, CancellationToken cancellationToken)
    {
        IReadOnlySet<string> Empty() => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (query.RoleCodes.Count == 0)
        {
            return Empty();
        }

        var roleCodes = query.RoleCodes.ToHashSet();
        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);
        var roleIds = roles
            .Where(r => roleCodes.Contains(r.Code.Value))
            .Select(r => r.Id.Value)
            .ToHashSet();
        if (roleIds.Count == 0)
        {
            return Empty();
        }

        var allVisibility = await visibilityRepository.ListAsync(
            new AllRoleMenuVisibilitiesSpec(), cancellationToken);
        var grantedLeafIds = allVisibility
            .Where(v => roleIds.Contains(v.RoleId.Value))
            .Select(v => v.MenuItemId.Value)
            .ToHashSet();

        var items = await menuRepository.ListAsync(new AllMenuItemsOrderedSpec(), cancellationToken);
        return items
            .Where(m => grantedLeafIds.Contains(m.Id.Value) && m.Route is not null)
            .Select(m => m.Route!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
