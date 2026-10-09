using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Users;

/// <summary>新增使用者（US3，FR-011/012/016）。帳號正規化查重、加鹽雜湊密碼、指派 ≥1 角色。</summary>
public sealed record CreateUserCommand(
    string Account, string DisplayName, string Password, IReadOnlyList<Guid> RoleIds)
    : ICommand<Result<Guid>>;

public sealed class CreateUserHandler(
    IRepository<User> userRepository,
    IReadRepository<Role> roleRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
    : ICommandHandler<CreateUserCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(
        CreateUserCommand command, CancellationToken cancellationToken)
    {
        var account = UserAccount.From(command.Account);

        var exists = await userRepository.AnyAsync(new UserByAccountSpec(account), cancellationToken);
        if (exists)
        {
            return Result<Guid>.Invalid(new ValidationError
            {
                Identifier = nameof(command.Account),
                ErrorMessage = "帳號已存在。",
            });
        }

        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);
        // roleIds 為角色對外識別碼（SPEC-064），轉為內部 Id；查無對應者視為無效角色。
        var roleIdByPublicId = roles.ToDictionary(r => r.PublicId.Value, r => r.Id);
        var requestedRoleIds = command.RoleIds.Distinct().ToList();
        if (requestedRoleIds.Count == 0 || requestedRoleIds.Any(id => !roleIdByPublicId.ContainsKey(id)))
        {
            return Result<Guid>.Invalid(new ValidationError
            {
                Identifier = nameof(command.RoleIds),
                ErrorMessage = "須指派至少一個有效角色。",
            });
        }

        var validRoleIds = requestedRoleIds.Select(id => roleIdByPublicId[id]).ToList();

        var (hash, salt) = passwordHasher.Hash(command.Password);
        var user = new User(account, UserName.From(command.DisplayName))
            .SetPassword(hash, salt, timeProvider.GetLocalNow().DateTime)
            .ReplaceRoles(validRoleIds);

        await userRepository.AddAsync(user, cancellationToken);
        return Result<Guid>.Success(user.PublicId.Value);
    }
}
