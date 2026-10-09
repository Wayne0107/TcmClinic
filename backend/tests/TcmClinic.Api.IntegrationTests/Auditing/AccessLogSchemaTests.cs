using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Auditing;
using TcmClinic.Api.Infrastructure.Data.Interceptors;

namespace TcmClinic.Api.IntegrationTests.Auditing;

/// <summary>
/// SPEC-010 綱要與五欄填入（設計決策 R7，2026-09-30 修訂）：append-only SysAccessLog 的型別、可空、說明與索引
/// （data-model；DATABASE-SCHEMA §1、§6、§8），以及 AccessLogRecorder 寫入時由攔截器填入五個標準欄位。
/// </summary>
public class AccessLogSchemaTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>sys.columns 查詢結果（MaxLength 為位元組數，nvarchar 每字 2 位元組）。</summary>
    private sealed class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public short MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }
        public string? Description { get; set; }
    }

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
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task<Dictionary<string, ColumnInfo>> ColumnsAsync(string table)
    {
        await using var ctx = NewContext();
        var rows = await ctx.Database.SqlQueryRaw<ColumnInfo>(
            """
            SELECT c.name AS ColumnName, t.name AS TypeName, c.max_length AS MaxLength,
                   c.is_nullable AS IsNullable, c.is_identity AS IsIdentity,
                   CAST(ep.value AS nvarchar(4000)) AS Description
            FROM sys.columns c
            JOIN sys.types t ON c.user_type_id = t.user_type_id
            LEFT JOIN sys.extended_properties ep
              ON ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = 'MS_Description'
            WHERE c.object_id = OBJECT_ID({0})
            """, table).ToListAsync(Ct);
        return rows.ToDictionary(r => r.ColumnName);
    }

    private async Task<string?> TableDescriptionAsync(string table)
    {
        await using var ctx = NewContext();
        return (await ctx.Database.SqlQueryRaw<string>(
            """
            SELECT CAST(ep.value AS nvarchar(4000)) AS Value
            FROM sys.extended_properties ep
            WHERE ep.major_id = OBJECT_ID({0}) AND ep.minor_id = 0 AND ep.name = 'MS_Description'
            """, table).ToListAsync(Ct)).SingleOrDefault();
    }

    private async Task<HashSet<string>> IndexNamesAsync(string table)
    {
        await using var ctx = NewContext();
        var names = await ctx.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sys.indexes WHERE object_id = OBJECT_ID({0}) AND name IS NOT NULL",
            table).ToListAsync(Ct);
        return names.ToHashSet();
    }

    // 情境：SysAccessLog 為 append-only 訪問紀錄表：bigint identity 主鍵（DATABASE-SCHEMA §6 唯一例外）、
    // 五個標準欄位照常具備且不得另用 AccessedBy／AccessedAt、每欄與表附說明、三個查詢索引採 EF 預設命名。
    // @SPEC-010 @AC-10
    // @SPEC-010 @AC-11
    [Fact(DisplayName = "SysAccessLog 具五個標準欄位、說明與 EF 預設命名索引（SPEC-010，設計決策 R7）")]
    public async Task SysAccessLog_schema_matches_design()
    {
        var cols = await ColumnsAsync("SysAccessLog");

        Assert.Equal("bigint", cols["Id"].TypeName);
        Assert.True(cols["Id"].IsIdentity);

        foreach (var name in new[] { "CreateUser", "ModifyUser" })
        {
            Assert.Equal("nvarchar", cols[name].TypeName);
            Assert.Equal(100, cols[name].MaxLength); // 50 字
            Assert.False(cols[name].IsNullable);
        }

        foreach (var name in new[] { "CreateDate", "ModifyDate" })
        {
            Assert.Equal("datetime", cols[name].TypeName);
            Assert.False(cols[name].IsNullable);
        }

        Assert.Equal("bit", cols["Removed"].TypeName);
        Assert.False(cols["Removed"].IsNullable);

        Assert.DoesNotContain("AccessedBy", cols.Keys);
        Assert.DoesNotContain("AccessedAt", cols.Keys);

        Assert.All(cols.Values, c => Assert.False(string.IsNullOrWhiteSpace(c.Description)));
        Assert.False(string.IsNullOrWhiteSpace(await TableDescriptionAsync("SysAccessLog")));

        var indexes = await IndexNamesAsync("SysAccessLog");
        Assert.Contains("IX_SysAccessLog_EntityName_PrimaryKey_CreateDate", indexes);
        Assert.Contains("IX_SysAccessLog_InvolvesSensitiveData_CreateDate", indexes);
        Assert.Contains("IX_SysAccessLog_CreateUser_CreateDate", indexes);
        Assert.DoesNotContain("IX_SysAccessLog_Entity_Pk_Time", indexes);
        Assert.DoesNotContain("IX_SysAccessLog_Sensitive_Time", indexes);
        Assert.DoesNotContain("IX_SysAccessLog_Accessor_Time", indexes);
    }

    // 情境（設計決策 R7）：AccessLogRecorder 加入紀錄後才 SaveChanges，新增的 AccessLog 經過 AuditableEntityInterceptor，
    // 由其填入五個標準欄位：建立者為目前使用者、Modify* 與 Create* 同值、Removed 為 false。
    // @SPEC-010 @AC-1
    // @SPEC-010 @AC-11
    [Fact(DisplayName = "訪問紀錄五個標準欄位由攔截器填入：建立者為訪問者、修改者與時間同建立、未刪除（設計決策 R7）")]
    public async Task Recorded_access_log_has_standard_columns_filled()
    {
        await using var ctx = NewContext();
        var recorder = new AccessLogRecorder(ctx, new SystemCurrentUser(), NullLogger<AccessLogRecorder>.Instance);
        var before = DateTime.Now.AddSeconds(-1);

        // User 含 [SensitiveData] 欄位，一般讀取也會被記錄（FR-008）。
        await recorder.RecordAsync<User>(new AccessLogRequest(AccessType.Read, "User", "R7", 1), Ct);

        var log = await ctx.AccessLogs.AsNoTracking().SingleAsync(a => a.PrimaryKey == "R7", Ct);
        Assert.Equal("system", log.CreateUser);
        Assert.Equal(log.CreateUser, log.ModifyUser);
        Assert.True(log.CreateDate >= before);
        Assert.Equal(log.CreateDate, log.ModifyDate);
        Assert.False(log.Removed);
    }
}
