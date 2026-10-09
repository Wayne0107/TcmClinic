using Ardalis.Result;
using Ardalis.Specification;
using Microsoft.Extensions.Logging.Abstractions;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Auth;
using NSubstitute;

namespace TcmClinic.Api.UnitTests.Auth;

/// <summary>
/// refresh token 換新用例（SPEC-004 FR-021a、FR-021b）：輪替以條件式保存，兩個請求同時用同一個 refresh token 換新時，
/// 搶輸的一方不得覆蓋已輪替的值，比照寬限只換發 access token。
/// </summary>
public class RefreshSessionHandlerTests
{
    private const string Token = "refresh-token-1";

    private static (RefreshSessionHandler Handler, ISessionRotationStore Store) Build(bool rotationCommitted)
    {
        var now = TimeProvider.System.GetLocalNow().DateTime;
        var session = new UserSession(UserId.From(1), RefreshTokenValue.Hash(Token), now.AddHours(-1)) { Id = UserSessionId.From(1) };
        var user = new User(UserAccount.From("alice"), UserName.From("Alice")) { Id = UserId.From(1) }
            .ReplaceRoles([RoleId.From(1)]);
        var role = new Role(RoleCode.From("CLERK"), RoleName.From("經辦")) { Id = RoleId.From(1) };

        // MOCK: 用例只依賴 repository 與輪替保存的 port，資料庫行為由整合與功能測試涵蓋。
        var sessions = Substitute.For<IRepository<UserSession>>();
        sessions.FirstOrDefaultAsync(Arg.Any<ISpecification<UserSession>>(), Arg.Any<CancellationToken>()).Returns(session);
        var users = Substitute.For<IReadRepository<User>>();
        users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(user);
        var roles = Substitute.For<IReadRepository<Role>>();
        roles.ListAsync(Arg.Any<ISpecification<Role>>(), Arg.Any<CancellationToken>()).Returns([role]);
        var store = Substitute.For<ISessionRotationStore>();
        store.TryCommitRotationAsync(Arg.Any<UserSession>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rotationCommitted);

        var handler = new RefreshSessionHandler(
            sessions, users, roles, store, TimeProvider.System, NullLogger<RefreshSessionHandler>.Instance);
        return (handler, store);
    }

    // 情境：條件式保存成功（資料庫中仍是原本的 refresh token）→ 回傳新的 refresh token，並以原雜湊為條件保存。
    // @SPEC-004 @AC-33
    [Fact(DisplayName = "輪替保存成功時回傳新的 refresh token（SPEC-004 FR-021a）")]
    public async Task Committed_rotation_returns_new_refresh_token()
    {
        var (handler, store) = Build(rotationCommitted: true);

        var result = await handler.Handle(new RefreshSessionCommand(Token), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value.NewRefreshToken);
        await store.Received(1).TryCommitRotationAsync(
            Arg.Any<UserSession>(), RefreshTokenValue.Hash(Token), Arg.Any<CancellationToken>());
    }

    // 情境：另一個並行請求已先輪替（條件式保存影響 0 筆）→ 不覆蓋，比照寬限只換發 access token、不回傳新的 refresh token。
    // @SPEC-004 @AC-41
    [Fact(DisplayName = "並行換新搶輸時不覆蓋輪替結果、只換發 access token（SPEC-004 FR-021b）")]
    public async Task Lost_concurrent_rotation_returns_access_token_only()
    {
        var (handler, _) = Build(rotationCommitted: false);

        var result = await handler.Handle(new RefreshSessionCommand(Token), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.Value.NewRefreshToken);
    }
}
