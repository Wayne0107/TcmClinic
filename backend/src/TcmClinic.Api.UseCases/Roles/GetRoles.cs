using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Roles;

/// <summary>角色（清單依角色名稱排序）。</summary>
/// <param name="Id">角色對外識別碼（UUIDv7，SPEC-064）；新增或修改使用者時放入 roleIds 的即為此值。</param>
/// <param name="Code">角色代碼，最長 50 字；未刪除角色中唯一。</param>
/// <param name="Name">角色名稱，最長 20 字。</param>
/// <param name="Description">角色說明，最長 200 字；未填時為 null。</param>
public sealed record RoleDto(Guid Id, string Code, string Name, string? Description);

/// <summary>列出所有角色（供 US2 選角色、US4 管理，FR-008）。</summary>
public sealed record GetRolesQuery : IQuery<Result<IReadOnlyList<RoleDto>>>;

public sealed class GetRolesHandler(IReadRepository<Role> repository)
    : IQueryHandler<GetRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    public async ValueTask<Result<IReadOnlyList<RoleDto>>> Handle(
        GetRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await repository.ListAsync(new RolesOrderedSpec(), cancellationToken);

        IReadOnlyList<RoleDto> dtos = roles
            .Select(r => new RoleDto(r.PublicId.Value, r.Code.Value, r.Name.Value, r.Description))
            .ToList();

        return Result<IReadOnlyList<RoleDto>>.Success(dtos);
    }
}
