using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Users;

/// <summary>修改使用者名稱與角色（US4，FR-014/016）。005：不設防鎖死。</summary>
public sealed record UpdateUserCommand(Guid Id, string DisplayName, IReadOnlyList<Guid> RoleIds)
    : ICommand<Result>;

public sealed class UpdateUserHandler(
    IRepository<User> userRepository,
    IReadRepository<Role> roleRepository)
    : ICommandHandler<UpdateUserCommand, Result>
{
    public async ValueTask<Result> Handle(
        UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (!UserPublicId.TryFrom(command.Id, out var publicId))
        {
            return Result.NotFound();
        }

        var user = await userRepository.FirstOrDefaultAsync(
            new UserByPublicIdSpec(publicId), cancellationToken);
        if (user is null)
        {
            return Result.NotFound();
        }

        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);
        // roleIds 為角色對外識別碼（SPEC-064），轉為內部 Id；查無對應者視為無效角色。
        var roleIdByPublicId = roles.ToDictionary(r => r.PublicId.Value, r => r.Id);
        var requestedRoleIds = command.RoleIds.Distinct().ToList();
        if (requestedRoleIds.Count == 0 || requestedRoleIds.Any(id => !roleIdByPublicId.ContainsKey(id)))
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(command.RoleIds),
                ErrorMessage = "須指派至少一個有效角色。",
            });
        }

        var validRoleIds = requestedRoleIds.Select(id => roleIdByPublicId[id]).ToList();

        user.Rename(UserName.From(command.DisplayName)).ReplaceRoles(validRoleIds);
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}
