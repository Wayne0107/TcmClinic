using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;

namespace TcmClinic.Api.IntegrationTests.Auditing;

/// <summary>
/// SPEC-006 綱要（設計決策 R12，2026-09-30 修訂）：append-only SysAuditLog 的型別、可空、說明與索引
/// （data-model；DATABASE-SCHEMA §1、§6、§8）。
/// </summary>
public class AuditLogSchemaTests : IAsyncLifetime
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

    // 情境：SysAuditLog 為 append-only 異動紀錄表：bigint identity 主鍵（DATABASE-SCHEMA §6 唯一例外）、
    // 五個標準欄位照常具備且不得另用 OperatedBy／OperatedAt、每欄與表附說明、兩個查詢索引採 EF 預設命名。
    // @SPEC-006 @AC-11
    [Fact(DisplayName = "SysAuditLog 具五個標準欄位、說明與 EF 預設命名索引（SPEC-006，設計決策 R12）")]
    public async Task SysAuditLog_schema_matches_design()
    {
        var cols = await ColumnsAsync("SysAuditLog");

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

        Assert.DoesNotContain("OperatedBy", cols.Keys);
        Assert.DoesNotContain("OperatedAt", cols.Keys);

        Assert.All(cols.Values, c => Assert.False(string.IsNullOrWhiteSpace(c.Description)));
        Assert.False(string.IsNullOrWhiteSpace(await TableDescriptionAsync("SysAuditLog")));

        var indexes = await IndexNamesAsync("SysAuditLog");
        Assert.Contains("IX_SysAuditLog_EntityName_PrimaryKey_CreateDate", indexes);
        Assert.Contains("IX_SysAuditLog_CreateUser_CreateDate", indexes);
        Assert.DoesNotContain("IX_SysAuditLog_Entity_Pk_Time", indexes);
        Assert.DoesNotContain("IX_SysAuditLog_Operator_Time", indexes);
    }
}
