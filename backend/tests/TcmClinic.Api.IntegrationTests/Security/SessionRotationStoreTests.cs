using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Security;

/// <summary>
/// session 輪替的條件式保存（SPEC-004 FR-021b，AC-41）：兩個請求各自讀到同一筆 session 並輪替，只有先保存者寫入成功，
/// 後保存者不得覆蓋（影響 0 筆、回 false），其追蹤中的實體改為資料庫中的最新值。
/// </summary>
public class SessionRotationStoreTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_db.GetConnectionString())
            .AddInterceptors(new AuditableEntityInterceptor(new SystemCurrentUser()))
            .Options;
        return new AppDbContext(options);
    }

    private SessionRotationStore Store(AppDbContext ctx) => new(ctx, new SystemCurrentUser(), TimeProvider.System);

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();
        await using var ctx = NewContext();
        await ctx.Database.MigrateAsync(Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // 情境：A、B 兩個請求同時以 h1 換新，A 先保存成功、B 後保存失敗；資料庫保留 A 的值，B 的實體重新載入為 A 的值。
    // @SPEC-004 @AC-41
    [Fact(DisplayName = "並行輪替只有先保存者成功、後者不覆蓋（SPEC-004 FR-021b）")]
    public async Task Concurrent_rotation_commits_only_once()
    {
        var now = DateTime.Now;
        UserSessionId id;
        await using (var seed = NewContext())
        {
            var user = new User(UserAccount.From("rotation01"), UserName.From("rotation01")).SetPassword("hash", "salt", now);
            seed.Users.Add(user);
            await seed.SaveChangesAsync(Ct);
            var session = new UserSession(user.Id, "h1", now);
            seed.UserSessions.Add(session);
            await seed.SaveChangesAsync(Ct);
            id = session.Id;
        }

        await using var ctxA = NewContext();
        await using var ctxB = NewContext();
        var a = await ctxA.UserSessions.SingleAsync(s => s.Id == id, Ct);
        var b = await ctxB.UserSessions.SingleAsync(s => s.Id == id, Ct);
        a.Rotate("h2a", now.AddSeconds(1));
        b.Rotate("h2b", now.AddSeconds(1));

        Assert.True(await Store(ctxA).TryCommitRotationAsync(a, "h1", Ct));
        Assert.False(await Store(ctxB).TryCommitRotationAsync(b, "h1", Ct));

        await using var verify = NewContext();
        var stored = await verify.UserSessions.AsNoTracking().SingleAsync(s => s.Id == id, Ct);
        Assert.Equal(("h2a", "h1"), (stored.TokenHash, stored.PreviousTokenHash));
        Assert.Equal("h2a", b.TokenHash);
    }
}
