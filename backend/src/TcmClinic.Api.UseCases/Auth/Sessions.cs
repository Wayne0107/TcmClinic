using Ardalis.Result;
using Mediator;
using Microsoft.Extensions.Logging;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.Core.SessionAggregate.Specifications;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Auth;

/// <summary>
/// refresh token 換新結果（SPEC-004 FR-021a／FR-021b）。<paramref name="NewRefreshToken"/> 為 null 表示 30 秒寬限內的並行換新，
/// 不輪替、用戶端沿用目前的 refresh Cookie。
/// </summary>
public sealed record RefreshResult(
    string Account,
    string DisplayName,
    IReadOnlyList<string> RoleCodes,
    Guid UserPublicId,
    Guid SessionId,
    string? NewRefreshToken,
    DateTime ExpiresAt);

/// <summary>以 refresh token 換發 access token 並輪替（FR-021a、FR-021b）。會寫入 session，故為 Command。</summary>
public sealed record RefreshSessionCommand(string? RefreshToken) : ICommand<Result<RefreshResult>>;

public sealed class RefreshSessionHandler(
    IRepository<UserSession> sessionRepository,
    IReadRepository<User> userRepository,
    IReadRepository<Role> roleRepository,
    ISessionRotationStore rotationStore,
    TimeProvider timeProvider,
    ILogger<RefreshSessionHandler> logger)
    : ICommandHandler<RefreshSessionCommand, Result<RefreshResult>>
{
    public async ValueTask<Result<RefreshResult>> Handle(
        RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return Result<RefreshResult>.Unauthorized();
        }

        var hash = RefreshTokenValue.Hash(command.RefreshToken);
        var session = await sessionRepository.FirstOrDefaultAsync(new UserSessionByTokenHashSpec(hash), cancellationToken);
        var now = timeProvider.GetLocalNow().DateTime;
        if (session is null || !session.IsActive(now))
        {
            return Result<RefreshResult>.Unauthorized();
        }

        var user = await userRepository.FirstOrDefaultAsync(new UserByIdWithRolesSpec(session.UserId), cancellationToken);
        var codes = user is null ? [] : await RoleCodesAsync(user, cancellationToken);
        if (user is null || codes.Count == 0)
        {
            // 帳號已刪除或無有效角色：撤銷 session（FR-021a）。
            session.Revoke(UserSession.RevokeReasons.UserInvalid, now);
            await sessionRepository.SaveChangesAsync(cancellationToken);
            return Result<RefreshResult>.Unauthorized();
        }

        string? newToken = null;
        if (session.PreviousTokenHash == hash)
        {
            if (!session.IsWithinReuseGrace(now))
            {
                // 寬限後重用已輪替的 refresh token：視為外洩，撤銷整個 session（FR-021b）。
                session.Revoke(UserSession.RevokeReasons.Reuse, now);
                await sessionRepository.SaveChangesAsync(cancellationToken);
                logger.LogWarning("[REFRESH_TOKEN_REUSE] 帳號 {Account} 重用已輪替的 refresh token，session 已撤銷", user.Account.Value);
                return Result<RefreshResult>.Unauthorized();
            }

            // 寬限內：多分頁並行換新，只換發 access token、不輪替。
        }
        else
        {
            var (token, newHash) = RefreshTokenValue.Generate();
            session.Rotate(newHash, now);
            // 條件式保存：另一個並行請求已先輪替時不覆蓋其結果，比照寬限只換發 access token（FR-021b）。
            if (await rotationStore.TryCommitRotationAsync(session, hash, cancellationToken))
            {
                newToken = token;
            }
        }

        return Result<RefreshResult>.Success(new RefreshResult(
            user.Account.Value, user.DisplayName.Value, codes, user.PublicId.Value,
            session.PublicId.Value, newToken, session.ExpiresAt));
    }

    private async Task<IReadOnlyList<string>> RoleCodesAsync(User user, CancellationToken cancellationToken)
    {
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        if (roleIds.Count == 0)
        {
            return [];
        }

        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);
        return roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Code.Value).ToList();
    }
}

/// <summary>
/// 登出（FR-005）：撤銷 refresh token 或 access token 的 sid 所指的 session。冪等，找不到也視為成功。
/// </summary>
public sealed record LogoutCommand(string? RefreshToken, Guid? SessionId) : ICommand<Result>;

public sealed class LogoutHandler(IRepository<UserSession> sessionRepository, TimeProvider timeProvider)
    : ICommandHandler<LogoutCommand, Result>
{
    public async ValueTask<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var sessions = new List<UserSession>();
        if (!string.IsNullOrEmpty(command.RefreshToken)
            && await sessionRepository.FirstOrDefaultAsync(
                new UserSessionByTokenHashSpec(RefreshTokenValue.Hash(command.RefreshToken)), cancellationToken) is { } byToken)
        {
            sessions.Add(byToken);
        }

        if (command.SessionId is { } sid
            && UserSessionPublicId.TryFrom(sid, out var publicId)
            && await sessionRepository.FirstOrDefaultAsync(new UserSessionByPublicIdSpec(publicId), cancellationToken) is { } bySid
            && !sessions.Contains(bySid))
        {
            sessions.Add(bySid);
        }

        if (sessions.Count == 0)
        {
            return Result.Success();
        }

        foreach (var session in sessions)
        {
            session.Revoke(UserSession.RevokeReasons.Logout, now);
        }

        await sessionRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// access token 所屬 session 是否仍有效（未撤銷、未逾期），且屬於 token 的使用者（<c>sub</c>，FR-021c）。供每請求驗證。
/// 比對使用者對外識別碼，帳號刪除後重建同名帳號時，舊 token 不會帶著新帳號的權限通過。
/// </summary>
public sealed record IsSessionActiveQuery(Guid SessionId, Guid UserPublicId) : IQuery<bool>;

public sealed class IsSessionActiveHandler(
    IReadRepository<UserSession> sessionRepository,
    IReadRepository<User> userRepository,
    TimeProvider timeProvider)
    : IQueryHandler<IsSessionActiveQuery, bool>
{
    public async ValueTask<bool> Handle(IsSessionActiveQuery query, CancellationToken cancellationToken)
    {
        if (!UserSessionPublicId.TryFrom(query.SessionId, out var sessionId)
            || !UserPublicId.TryFrom(query.UserPublicId, out var userId))
        {
            return false;
        }

        var session = await sessionRepository.FirstOrDefaultAsync(
            new UserSessionByPublicIdForReadSpec(sessionId), cancellationToken);
        if (session is null || !session.IsActive(timeProvider.GetLocalNow().DateTime))
        {
            return false;
        }

        // 全域過濾器排除軟刪除者：帳號已刪除時查無使用者。
        var user = await userRepository.FirstOrDefaultAsync(new UserByPublicIdSpec(userId), cancellationToken);
        return user is not null && user.Id == session.UserId;
    }
}
