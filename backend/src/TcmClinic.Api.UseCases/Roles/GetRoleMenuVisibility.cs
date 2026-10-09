using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Roles;

/// <summary>
/// 角色可見選單樹的節點；以 Children 是否為空區分容器與葉節點，只有葉節點可被授權。
/// </summary>
/// <remarks>
/// 容器/葉之區分由消費端以 <see cref="Children"/> 是否為空判定，故不外露 IsContainer 欄位。
/// </remarks>
/// <param name="Id">選單項目對外識別碼（UUIDv7，SPEC-064）；設定可見選單時放入 grantedMenuItemIds 的即為此值。</param>
/// <param name="Name">選單名稱。</param>
/// <param name="Route">前端選單識別鍵（路由／value）；未設定時為 null。</param>
/// <param name="Granted">此角色是否已被授權此選單；僅葉節點可能為 true，容器一律為 false。</param>
/// <param name="Children">子節點，依顯示順序排列；葉節點為空陣列。</param>
public sealed record RoleMenuVisibilityNodeDto(
    Guid Id,
    string Name,
    string? Route,
    bool Granted,
    IReadOnlyList<RoleMenuVisibilityNodeDto> Children);

/// <summary>取得可見選單的結果（012：附角色並行權杖，供設定權限儲存時回帶）。</summary>
public sealed record RoleMenuVisibilityResult(
    string RowVersion, IReadOnlyList<RoleMenuVisibilityNodeDto> Nodes);

/// <summary>取得某角色的可見選單設定（US2/US3, FR-008/015）。</summary>
public sealed record GetRoleMenuVisibilityQuery(Guid RoleId)
    : IQuery<Result<RoleMenuVisibilityResult>>;

public sealed class GetRoleMenuVisibilityHandler(
    IReadRepository<Role> roleRepository,
    IReadRepository<MenuItem> menuRepository,
    IReadRepository<RoleMenuVisibility> visibilityRepository)
    : IQueryHandler<GetRoleMenuVisibilityQuery, Result<RoleMenuVisibilityResult>>
{
    public async ValueTask<Result<RoleMenuVisibilityResult>> Handle(
        GetRoleMenuVisibilityQuery query, CancellationToken cancellationToken)
    {
        if (!RolePublicId.TryFrom(query.RoleId, out var publicId))
        {
            return Result<RoleMenuVisibilityResult>.NotFound();
        }

        var role = await roleRepository.FirstOrDefaultAsync(new RoleByPublicIdSpec(publicId), cancellationToken);
        if (role is null)
        {
            return Result<RoleMenuVisibilityResult>.NotFound();
        }

        var roleId = role.Id;

        var items = await menuRepository.ListAsync(new AllMenuItemsOrderedSpec(), cancellationToken);
        var tree = MenuTree.Build(items);

        var granted = await visibilityRepository.ListAsync(
            new RoleMenuVisibilityByRoleSpec(roleId), cancellationToken);
        var grantedIds = granted.Select(v => v.MenuItemId.Value).ToHashSet();

        return Result<RoleMenuVisibilityResult>.Success(
            new RoleMenuVisibilityResult(Convert.ToBase64String(role.RowVersion), Map(tree, grantedIds)));
    }

    private static IReadOnlyList<RoleMenuVisibilityNodeDto> Map(
        IReadOnlyList<MenuTreeNode> nodes, HashSet<int> grantedIds) =>
        nodes.Select(n => new RoleMenuVisibilityNodeDto(
            n.Item.PublicId.Value,
            n.Item.Name.Value,
            n.Item.Route,
            // 僅葉節點（無子節點）可為已授權；容器以「有無子節點」判定，對齊前端選單。
            n.Children.Count == 0 && grantedIds.Contains(n.Item.Id.Value),
            Map(n.Children, grantedIds))).ToList();
}
