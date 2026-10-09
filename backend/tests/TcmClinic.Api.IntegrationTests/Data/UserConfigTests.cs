using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Data;

/// <summary>SysUser 映射：帳號過濾唯一索引、軟刪除重用、稽核欄位（T024，FR-012、US5 情境 3）。</summary>
public class UserConfigTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();

    private readonly Pbkdf2PasswordHasher _hasher = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_db.GetConnectionString())
            .AddInterceptors(new AuditableEntityInterceptor(new SystemCurrentUser()))
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

    private async Task<RoleId> SuperAdminRoleIdAsync(AppDbContext ctx)
    {
        var role = await ctx.Roles.IgnoreQueryFilters()
            .FirstAsync(r => r.Code == RoleCode.From(Role.SuperAdminCode), Ct);
        return role.Id;
    }

    private User NewUser(RoleId roleId, string account, string displayName)
    {
        var (hash, salt) = _hasher.Hash("P@ssw0rd1234");
        return new User(UserAccount.From(account), UserName.From(displayName))
            .SetPassword(hash, salt, DateTime.Now)
            .ReplaceRoles([roleId]);
    }

    // 情境：大小寫不同的重複帳號（正規化後同為 "sam"）→ 過濾唯一索引拒絕（FR-012）。
    // @SPEC-004 @AC-11
    [Fact(DisplayName = "大小寫不同的重複帳號經正規化後由過濾唯一索引拒絕（FR-012）")]
    public async Task Duplicate_account_case_insensitive_is_rejected()
    {
        await using (var ctx = NewContext())
        {
            ctx.Users.Add(NewUser(await SuperAdminRoleIdAsync(ctx), "sam", "Sam"));
            await ctx.SaveChangesAsync(Ct);
        }

        await using (var ctx = NewContext())
        {
            ctx.Users.Add(NewUser(await SuperAdminRoleIdAsync(ctx), "SAM", "Sam2"));
            await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync(Ct));
        }
    }

    // 情境：軟刪除後同帳號可重用，且稽核欄位由攔截器填入（US5 情境 3、Principle XI）。
    // @SPEC-004 @AC-19
    // @SPEC-004 @AC-25
    [Fact(DisplayName = "軟刪除後同帳號可重用且稽核欄位由攔截器填入（US5 情境 3、Principle XI）")]
    public async Task Soft_deleted_account_can_be_reused_and_audit_filled()
    {
        int id;
        await using (var ctx = NewContext())
        {
            var user = NewUser(await SuperAdminRoleIdAsync(ctx), "tom", "Tom");
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync(Ct);
            id = user.Id.Value;

            Assert.NotEqual(default, user.CreateDate);
            Assert.Equal("system", user.CreateUser);
        }

        await using (var ctx = NewContext())
        {
            var user = await ctx.Users.FirstAsync(u => u.Id == UserId.From(id), Ct);
            ctx.Users.Remove(user);
            await ctx.SaveChangesAsync(Ct);
        }

        await using (var ctx = NewContext())
        {
            ctx.Users.Add(NewUser(await SuperAdminRoleIdAsync(ctx), "tom", "Tom2"));
            await ctx.SaveChangesAsync(Ct);
        }

        await using (var verify = NewContext())
        {
            // 全域過濾器只回未刪除者，應僅剩重用後的 "tom"（Tom2）。
            var active = await verify.Users
                .Where(u => u.Account == UserAccount.From("tom"))
                .ToListAsync(Ct);

            Assert.Single(active);
            Assert.Equal("Tom2", active[0].DisplayName.Value);
        }
    }
}
