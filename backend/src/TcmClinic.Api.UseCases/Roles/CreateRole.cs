using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Roles;

/// <summary>
/// 新增角色（US4/US2，FR-005/006/007）。012：新增 <see cref="RoleType"/>，僅接受 Custom——System 類僅由種子建立（如 SM），
/// 因其不可刪且無修改種類之途徑（FR-004，clarify 2026-09-30）；<see cref="Code"/> 選填——
/// 提供則沿用（向後相容），省略則由後端自動產生不重複的 R### 代碼。
/// </summary>
public sealed record CreateRoleCommand(string? Code, string Name, string? Description, string RoleType)
    : ICommand<Result<Guid>>;

public sealed class CreateRoleHandler(IRepository<Role> repository)
    : ICommandHandler<CreateRoleCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(
        CreateRoleCommand command, CancellationToken cancellationToken)
    {
        // 角色種類（012，FR-004）：新增僅接受 Custom；System 或非法值一律拒絕。
        if (!RoleType.TryFromName(command.RoleType, out var roleType) || roleType != RoleType.Custom)
        {
            return Result<Guid>.Invalid(new ValidationError
            {
                Identifier = nameof(command.RoleType),
                ErrorMessage = "新增角色僅能為一般使用者類（Custom）。",
            });
        }

        // 代碼：提供則沿用，否則自動產生下一個 R###（012，FR-007）。
        // 產碼跨未刪除與已軟刪除列掃描，避免回收已刪除角色的代碼（設計決策 R5）。
        string codeValue;
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            var allRoles = await repository.ListAsync(new AllRolesIncludingDeletedSpec(), cancellationToken);
            codeValue = RoleCodeGenerator.Next(allRoles.Select(r => r.Code.Value));
        }
        else
        {
            codeValue = command.Code.Trim();
        }
        var code = RoleCode.From(codeValue);

        var exists = await repository.AnyAsync(new RoleByCodeSpec(code), cancellationToken);
        if (exists)
        {
            return Result<Guid>.Invalid(new ValidationError
            {
                Identifier = nameof(command.Code),
                ErrorMessage = "角色代碼已存在。",
            });
        }

        var role = new Role(code, RoleName.From(command.Name), command.Description, roleType);
        await repository.AddAsync(role, cancellationToken);

        // 回傳對外識別碼（SPEC-064），不公開整數主鍵。
        return Result<Guid>.Success(role.PublicId.Value);
    }
}
