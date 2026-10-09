using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Paging;

namespace TcmClinic.Api.UseCases.Roles;

/// <summary>角色權限維護清單中的一筆角色。</summary>
/// <remarks>012，含角色種類、是否已指派、並行權杖。</remarks>
/// <param name="Id">角色對外識別碼（UUIDv7，SPEC-064）。</param>
/// <param name="Code">角色代碼，最長 50 字；未刪除角色中唯一。</param>
/// <param name="Name">角色名稱，最長 20 字。</param>
/// <param name="Description">角色說明，最長 200 字；未填時為 null。</param>
/// <param name="RoleType">角色種類：<c>System</c>（系統管理員類，不可刪除）或 <c>Custom</c>（一般使用者類）。</param>
/// <param name="IsAssigned">是否已被任一使用者指派；已指派的角色不可刪除。</param>
/// <param name="RowVersion">角色並行權杖（base64 字串）；設定可見選單時原樣回帶於 rowVersion。</param>
public sealed record RoleMaintenanceItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string RoleType,
    bool IsAssigned,
    string RowVersion);

/// <summary>角色權限維護清單的分頁結果。</summary>
/// <remarks>比照 011 的 LcApplicationListResult。</remarks>
/// <param name="Items">本頁角色，依角色名稱排序。</param>
/// <param name="Page">實際套用的頁碼（從 1 起算）。</param>
/// <param name="PageSize">實際套用的每頁筆數（1～100）。</param>
/// <param name="TotalCount">未刪除角色總筆數。</param>
public sealed record RoleMaintenanceListResult(
    IReadOnlyList<RoleMaintenanceItemDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>角色權限維護頁清單查詢（US1，FR-001/002/003/004）。分頁，無搜尋。</summary>
public sealed record GetRolesForMaintenanceQuery(int Page, int? PageSize)
    : IQuery<Result<RoleMaintenanceListResult>>;

public sealed class GetRolesForMaintenanceHandler(
    IReadRepository<Role> roleRepository,
    IReadRepository<User> userRepository)
    : IQueryHandler<GetRolesForMaintenanceQuery, Result<RoleMaintenanceListResult>>
{
    public async ValueTask<Result<RoleMaintenanceListResult>> Handle(
        GetRolesForMaintenanceQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        // 未指定、0 或負數時每頁 10 筆，指定時上限 100（ADR-007）。
        var pageSize = PageSizeResolver.Resolve(query.PageSize);

        // 角色數量少（後台規模），依名稱排序後於記憶體分頁（比照 011）。
        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);

        // 單次讀取所有（未刪除）使用者含其角色指派，彙總為「已指派角色 id 集合」，避免 N+1（Principle VI）。
        var users = await userRepository.ListAsync(new UsersOrderedSpec(), cancellationToken);
        var assignedRoleIds = users
            .SelectMany(u => u.UserRoles)
            .Select(ur => ur.RoleId.Value)
            .ToHashSet();

        var total = roles.Count;

        var items = roles
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleMaintenanceItemDto(
                r.PublicId.Value,
                r.Code.Value,
                r.Name.Value,
                r.Description,
                r.RoleType.Name,
                assignedRoleIds.Contains(r.Id.Value),
                Convert.ToBase64String(r.RowVersion)))
            .ToList();

        return Result<RoleMaintenanceListResult>.Success(
            new RoleMaintenanceListResult(items, page, pageSize, total));
    }
}
