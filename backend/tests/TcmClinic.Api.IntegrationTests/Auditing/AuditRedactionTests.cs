using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Auditing;

/// <summary>
/// 機密遮罩護欄（FR-013/SC-002、Principle XVII）：密碼雜湊與 salt 絕不出現在異動紀錄快照，
/// 一般個資（帳號）則照實記錄（clarify Q3）。此測試若失敗即代表遮罩迴歸，須擋下。
/// </summary>
public class AuditRedactionTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();

    private readonly Pbkdf2PasswordHasher _hasher = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AppDbContext NewContext()
    {
        var currentUser = new SystemCurrentUser();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_db.GetConnectionString())
            .AddInterceptors(
                new AuditableEntityInterceptor(currentUser),
                new AuditLogInterceptor(currentUser))
            .Options;
        return new AppDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();
        await using var ctx = NewContext();
        await ctx.Database.MigrateAsync(Ct);
        await SeedData.SeedAsync(ctx, _hasher, "Admin123!", Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // 情境：建立含密碼的使用者 → 快照中密碼雜湊/salt 被遮罩，帳號照實。
    // @SPEC-006 @AC-4
    [Fact(DisplayName = "使用者密碼雜湊與 salt 於異動紀錄中被遮罩")]
    public async Task Password_hash_and_salt_are_masked_in_audit()
    {
        var (hash, salt) = _hasher.Hash("Secret123!");

        await using var ctx = NewContext();
        var user = new User(UserAccount.From("audituser"), UserName.From("稽核使用者"))
            .SetPassword(hash, salt, DateTime.Now);
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(Ct);

        var pk = user.Id.Value.ToString();
        var log = await ctx.AuditLogs.SingleAsync(
            a => a.EntityName == "User" && a.PrimaryKey == pk && a.Operation == "Insert", Ct);

        Assert.NotNull(log.NewValues);
        Assert.Contains("***", log.NewValues);
        Assert.DoesNotContain(hash, log.NewValues);
        Assert.DoesNotContain(salt, log.NewValues);
        // 一般個資（帳號）照實記錄（clarify Q3=A）。
        Assert.Contains("audituser", log.NewValues);
    }
}
