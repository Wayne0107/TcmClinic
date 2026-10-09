using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Auth;

/// <summary>
/// 使用者是否仍存在（未刪除）、其現時角色代碼與顯示名稱。供每請求重驗（R8）；
/// 顯示名稱供刷新 GivenName claim，使改名後 /auth/me 立即反映（SPEC-013，設計決策 R12）。
/// </summary>
public sealed record UserRolesResult(bool Exists, IReadOnlyList<string> RoleCodes, string? DisplayName = null);

/// <summary>依帳號取得現時角色（R8：Cookie OnValidatePrincipal 每請求刷新，FR-015/刪除邊界）。</summary>
public sealed record GetUserRolesByAccountQuery(string Account) : IQuery<UserRolesResult>;

public sealed class GetUserRolesByAccountHandler(
    IReadRepository<User> userRepository,
    IReadRepository<Role> roleRepository)
    : IQueryHandler<GetUserRolesByAccountQuery, UserRolesResult>
{
    public async ValueTask<UserRolesResult> Handle(
        GetUserRolesByAccountQuery query, CancellationToken cancellationToken)
    {
        UserAccount account;
        try
        {
            account = UserAccount.From(query.Account ?? string.Empty);
        }
        catch (Vogen.ValueObjectValidationException)
        {
            return new UserRolesResult(false, []);
        }

        // 全域過濾器排除軟刪除者 → 被刪除的使用者視為不存在（R8 撤權/刪除即時失效）。
        var user = await userRepository.FirstOrDefaultAsync(new UserByAccountSpec(account), cancellationToken);
        if (user is null)
        {
            return new UserRolesResult(false, []);
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        if (roleIds.Count == 0)
        {
            return new UserRolesResult(true, [], user.DisplayName.Value);
        }

        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);
        var codes = roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Code.Value).ToList();
        return new UserRolesResult(true, codes, user.DisplayName.Value);
    }
}
