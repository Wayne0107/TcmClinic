using Ardalis.Result;
using Mediator;
using Microsoft.Extensions.Logging;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Auth;

/// <summary>登入建立的 session（SPEC-004 FR-021，ADR-011）：sid、交給用戶端的 refresh token 原值與到期時間（本地時間）。</summary>
public sealed record SessionTokens(Guid SessionId, string RefreshToken, DateTime ExpiresAt);

/// <summary>登入成功後回傳的身分摘要（帳號、顯示名稱、角色代碼、使用者對外識別碼）與新建的 session。</summary>
public sealed record LoginResult(
    string Account, string DisplayName, IReadOnlyList<string> RoleCodes, Guid UserPublicId, SessionTokens Session);

/// <summary>
/// 以帳號 + 密碼登入（US1，FR-002/003/006/006a）。含登入失敗鎖定、舊密碼雜湊轉換（FR-007a）與建立 session（FR-021）副作用，故為 Command。
/// </summary>
public sealed record LoginCommand(string Account, string Password) : ICommand<Result<LoginResult>>;

public sealed class LoginHandler(
    IRepository<User> userRepository,
    IReadRepository<Role> roleRepository,
    IRepository<UserSession> sessionRepository,
    IPasswordHasher passwordHasher,
    ILoginThrottle throttle,
    TimeProvider timeProvider,
    ILogger<LoginHandler> logger)
    : ICommandHandler<LoginCommand, Result<LoginResult>>
{
    public async ValueTask<Result<LoginResult>> Handle(
        LoginCommand command, CancellationToken cancellationToken)
    {
        UserAccount account;
        try
        {
            account = UserAccount.From(command.Account ?? string.Empty);
        }
        catch (Vogen.ValueObjectValidationException)
        {
            // 一般性失敗，不透露帳號格式問題（FR-003）。
            return Result<LoginResult>.Unauthorized();
        }

        var key = account.Value;
        if (throttle.IsLocked(key))
        {
            // 鎖定期間即使帳密正確亦拒絕（FR-006a）。
            return Result<LoginResult>.Unauthorized();
        }

        // 全域查詢過濾器已排除軟刪除帳號（FR-006）。
        var user = await userRepository.FirstOrDefaultAsync(new UserByAccountSpec(account), cancellationToken);
        if (user is null
            || !passwordHasher.Verify(command.Password ?? string.Empty, user.PasswordHash, user.PasswordSalt))
        {
            throttle.RegisterFailure(key);
            return Result<LoginResult>.Unauthorized();
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        if (roleIds.Count == 0)
        {
            // 無任何有效角色不可登入（FR-006）。
            return Result<LoginResult>.Unauthorized();
        }

        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);
        var codes = roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Code.Value).ToList();
        if (codes.Count == 0)
        {
            return Result<LoginResult>.Unauthorized();
        }

        await UpgradePasswordHashAsync(user, command.Password ?? string.Empty, cancellationToken);
        throttle.Reset(key);

        // 建立 session（ADR-011）：只存 refresh token 的雜湊，原值交給端點以 HttpOnly Cookie 回給用戶端。
        var (refreshToken, tokenHash) = RefreshTokenValue.Generate();
        var session = new UserSession(user.Id, tokenHash, timeProvider.GetLocalNow().DateTime);
        await sessionRepository.AddAsync(session, cancellationToken);

        return Result<LoginResult>.Success(new LoginResult(
            account.Value, user.DisplayName.Value, codes, user.PublicId.Value,
            new SessionTokens(session.PublicId.Value, refreshToken, session.ExpiresAt)));
    }

    /// <summary>
    /// 登入成功時把舊雜湊（SHA-256 或較低迭代次數）轉為現行演算法（FR-007a、ADR-009），不變更密碼修改時間。
    /// 盡力而為：寫入失敗只記 Warning、不影響本次登入，下次登入會再嘗試。
    /// </summary>
    private async Task UpgradePasswordHashAsync(User user, string password, CancellationToken cancellationToken)
    {
        if (!passwordHasher.NeedsRehash(user.PasswordHash))
        {
            return;
        }

        try
        {
            // 登入查詢為 AsNoTracking 且含角色；另取追蹤中的實體只改雜湊欄位，避免連同角色關聯一併更新。
            var tracked = await userRepository.GetByIdAsync(user.Id, cancellationToken);
            if (tracked is null)
            {
                return;
            }

            var (hash, salt) = passwordHasher.Hash(password);
            tracked.UpgradePasswordHash(hash, salt);
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "[PASSWORD_REHASH] 帳號 {Account} 密碼雜湊轉換失敗，下次登入再試", user.Account.Value);
        }
    }
}
