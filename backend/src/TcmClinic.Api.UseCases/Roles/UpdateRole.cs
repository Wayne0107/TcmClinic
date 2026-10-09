using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Roles;

/// <summary>編輯角色名稱／說明（US4, FR-014）。角色代碼不可變更。</summary>
public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description)
    : ICommand<Result>;

public sealed class UpdateRoleHandler(IRepository<Role> repository)
    : ICommandHandler<UpdateRoleCommand, Result>
{
    public async ValueTask<Result> Handle(
        UpdateRoleCommand command, CancellationToken cancellationToken)
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

        role.UpdateName(RoleName.From(command.Name)).UpdateDescription(command.Description);
        await repository.UpdateAsync(role, cancellationToken);

        return Result.Success();
    }
}
