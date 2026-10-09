using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Auditing;

/// <summary>
/// AuditLogInterceptor 端到端（FR-001/002/003/006/007/008）：新增/修改/刪除產生正確紀錄與主鍵；
/// 同交易一致（回滾無孤兒）；無實質變更不記。
/// </summary>
public class AuditLogInterceptorTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();

    private readonly Pbkdf2PasswordHasher _hasher = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AppDbContext NewContext()
    {
        var currentUser = new SystemCurrentUser();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_db.GetConnectionString())
            // 順序同正式設定：Auditable 先、AuditLog 後。
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

    private static Role NewRole(string suffix) =>
        new(RoleCode.From($"AUDIT_{suffix}"), RoleName.From($"稽核{suffix}"), "稽核測試角色");

    // 情境：新增資料 → 一筆 Insert，OldValues 為空、正確主鍵、NewValues 含內容（US1-2/FR-007）。
    // @SPEC-006 @AC-2
    [Fact(DisplayName = "新增資料產生 Insert 紀錄且主鍵正確")]
    public async Task Insert_creates_audit_record_with_pk()
    {
        await using var ctx = NewContext();
        var role = NewRole("INS");
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync(Ct);

        var pk = role.Id.Value.ToString();
        var log = await ctx.AuditLogs.SingleAsync(
            a => a.EntityName == "Role" && a.PrimaryKey == pk && a.Operation == "Insert", Ct);

        Assert.Null(log.OldValues);
        Assert.NotNull(log.NewValues);
        Assert.Contains("AUDIT_INS", log.NewValues);
        Assert.Equal("system", log.CreateUser);
    }

    // 情境（設計決策 R12，2026-09-30 修訂）：異動紀錄的五個標準欄位由 AuditLogInterceptor 填入——
    // 本攔截器在 AuditableEntityInterceptor 之後，新增的 AuditLog 不會再經過後者，故 Modify* 須與 Create* 同值、Removed 為 false。
    // @SPEC-006 @AC-11
    [Fact(DisplayName = "異動紀錄五個標準欄位：建立者為操作者、修改者與時間同建立、未刪除（設計決策 R12）")]
    public async Task Audit_record_has_standard_columns_filled()
    {
        await using var ctx = NewContext();
        var before = DateTime.Now.AddSeconds(-1);
        var role = NewRole("STD");
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync(Ct);

        var pk = role.Id.Value.ToString();
        var log = await ctx.AuditLogs.AsNoTracking().SingleAsync(
            a => a.EntityName == "Role" && a.PrimaryKey == pk && a.Operation == "Insert", Ct);

        Assert.Equal("system", log.CreateUser);
        Assert.Equal(log.CreateUser, log.ModifyUser);
        Assert.True(log.CreateDate >= before);
        Assert.Equal(log.CreateDate, log.ModifyDate);
        Assert.False(log.Removed);
    }

    // 情境：修改欄位 → 一筆 Update，含改前/改後（US1-1/FR-003）。
    // @SPEC-006 @AC-1
    [Fact(DisplayName = "修改資料產生 Update 紀錄含改前改後")]
    public async Task Update_creates_audit_record_with_old_and_new()
    {
        await using var ctx = NewContext();
        var role = NewRole("UPD");
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync(Ct);

        role.UpdateName(RoleName.From("稽核已改名"));
        await ctx.SaveChangesAsync(Ct);

        var pk = role.Id.Value.ToString();
        var log = await ctx.AuditLogs
            .Where(a => a.EntityName == "Role" && a.PrimaryKey == pk && a.Operation == "Update")
            .OrderByDescending(a => a.Id)
            .FirstAsync(Ct);

        Assert.Contains("稽核UPD", log.OldValues!);
        Assert.Contains("稽核已改名", log.NewValues!);
    }

    // 情境：刪除（軟刪除）→ 一筆 Delete，保留刪除前值（US1-3）。
    // @SPEC-006 @AC-3
    [Fact(DisplayName = "刪除資料產生 Delete 紀錄")]
    public async Task Delete_creates_delete_record()
    {
        await using var ctx = NewContext();
        var role = NewRole("DEL");
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync(Ct);

        ctx.Roles.Remove(role);
        await ctx.SaveChangesAsync(Ct);

        var pk = role.Id.Value.ToString();
        var log = await ctx.AuditLogs.SingleAsync(
            a => a.EntityName == "Role" && a.PrimaryKey == pk && a.Operation == "Delete", Ct);

        Assert.Contains("AUDIT_DEL", log.OldValues!);
        Assert.Null(log.NewValues);
    }

    // 情境：資料變更回滾 → 不留孤兒紀錄（US1-5/FR-006/SC-004）。
    // @SPEC-006 @AC-5
    [Fact(DisplayName = "資料變更回滾後不留孤兒稽核紀錄")]
    public async Task Rollback_leaves_no_orphan_audit()
    {
        string pk;
        await using (var ctx = NewContext())
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(Ct);
            var role = NewRole("RB");
            ctx.Roles.Add(role);
            await ctx.SaveChangesAsync(Ct);
            pk = role.Id.Value.ToString();
            await tx.RollbackAsync(Ct);
        }

        await using var verify = NewContext();
        var count = await verify.AuditLogs
            .CountAsync(a => a.EntityName == "Role" && a.PrimaryKey == pk, Ct);
        Assert.Equal(0, count);
    }

    // 情境：無實質變更之存檔 → 不產生紀錄（FR-008）。
    // @SPEC-006 @AC-10
    [Fact(DisplayName = "無實質變更之存檔不產生紀錄")]
    public async Task Noop_save_creates_no_record()
    {
        await using var ctx = NewContext();
        var before = await ctx.AuditLogs.CountAsync(Ct);
        await ctx.SaveChangesAsync(Ct); // 無追蹤變更
        var after = await ctx.AuditLogs.CountAsync(Ct);
        Assert.Equal(before, after);
    }
}
