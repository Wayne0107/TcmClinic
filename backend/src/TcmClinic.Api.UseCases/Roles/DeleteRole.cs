using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Roles;

/// <summary>
/// 刪除（軟刪除）角色（US4，FR-011/012/013）。012：伺服器端強制保護——
/// System 類角色（如 SM）不可刪；已被任何使用者指派的角色不可刪。SUPERADMIN 種為 Custom，維持 005 可刪。
/// </summary>
public sealed record DeleteRoleCommand(Guid Id) : ICommand<Result>;

public sealed class DeleteRoleHandler(
    IRepository<Role> repository,
    IReadRepository<User> userRepository)
    : ICommandHandler<DeleteRoleCommand, Result>
{
    public async ValueTask<Result> Handle(
        DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        if (!RolePublicId.TryFrom(command.Id, out var publicId))
        {
            return Result.NotFound();
        }

        var role = await repository.FirstOrDefaultAsync(new RoleByPublicIdSpec(publicId), cancellationToken);
        if (role is null)
        {
            return Result.NotFound();
        }

        var roleId = role.Id;

        // FR-013：系統管理員類（System）角色不可刪除。
        if (role.IsSystemRole)
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(command.Id),
                ErrorMessage = "系統管理員類角色不可刪除。",
            });
        }

        // FR-012：已被任何使用者指派的角色不可刪除（伺服器端強制，不靠前端隱藏）。
        var users = await userRepository.ListAsync(new UsersOrderedSpec(), cancellationToken);
        if (users.SelectMany(u => u.UserRoles).Any(ur => ur.RoleId == roleId))
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(command.Id),
                ErrorMessage = "角色已被指派無法刪除。",
            });
        }

        await repository.DeleteAsync(role, cancellationToken); // 攔截器轉為軟刪除
        return Result.Success();
    }
}
