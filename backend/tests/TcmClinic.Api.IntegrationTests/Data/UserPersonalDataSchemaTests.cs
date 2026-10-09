using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.IntegrationTests.Data;

/// <summary>
/// SPEC-013 綱要：SysUser 新增三欄與 append-only SysUserLog 的型別、可空、說明與索引
/// （data-model §1.1、§4；DATABASE-SCHEMA §3 欄位說明）。
/// </summary>
public class UserPersonalDataSchemaTests : IAsyncLifetime
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

    // 情境：SysUser 新增聯絡電話、電子郵件、上次修改密碼時間三欄，皆可空且附繁中說明（data-model §1.1）。
    // @SPEC-013 @AC-33
    [Fact(DisplayName = "SysUser 新增 Phone/Email/PasswordChangedAt 可空欄位且附說明（SPEC-013）")]
    public async Task SysUser_has_personal_data_columns()
    {
        var cols = await ColumnsAsync("SysUser");

        Assert.Equal("nvarchar", cols["Phone"].TypeName);
        Assert.Equal(40, cols["Phone"].MaxLength); // 20 字
        Assert.True(cols["Phone"].IsNullable);

        Assert.Equal("nvarchar", cols["Email"].TypeName);
        Assert.Equal(200, cols["Email"].MaxLength); // 100 字
        Assert.True(cols["Email"].IsNullable);

        Assert.Equal("datetime", cols["PasswordChangedAt"].TypeName);
        Assert.True(cols["PasswordChangedAt"].IsNullable);

        Assert.All(new[] { "Phone", "Email", "PasswordChangedAt" },
            name => Assert.False(string.IsNullOrWhiteSpace(cols[name].Description)));
    }

    // 情境：SysUserLog 為 append-only 操作紀錄表：bigint identity 主鍵（DATABASE-SCHEMA §6 唯一例外）、
    // 五個標準欄位照常具備且不得另用 OperatedBy／OperatedAt、每欄與表附說明、兩個查詢索引採 EF 預設命名
    // （data-model §4、設計決策 R8 2026-09-30 修訂、DATABASE-SCHEMA §1／§8）。
    // @SPEC-013 @AC-33
    [Fact(DisplayName = "SysUserLog 具五個標準欄位、說明與 EF 預設命名索引（SPEC-013，FR-015、設計決策 R8）")]
    public async Task SysUserLog_schema_matches_design()
    {
        var cols = await ColumnsAsync("SysUserLog");

        Assert.Equal("bigint", cols["Id"].TypeName);
        Assert.True(cols["Id"].IsIdentity);

        (string Name, string Type, short Bytes, bool Nullable)[] expected =
        [
            ("FunctionId", "nvarchar", 100, false),
            ("Action", "nvarchar", 100, false),
            ("Status", "nvarchar", 40, false),
            ("Content", "nvarchar", 1000, false),
            ("ClientIp", "nvarchar", 90, true),
            ("CreateUser", "nvarchar", 100, false),
            ("ModifyUser", "nvarchar", 100, false),
        ];
        foreach (var (name, type, bytes, nullable) in expected)
        {
            Assert.Equal(type, cols[name].TypeName);
            Assert.Equal(bytes, cols[name].MaxLength);
            Assert.Equal(nullable, cols[name].IsNullable);
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
        Assert.False(string.IsNullOrWhiteSpace(await TableDescriptionAsync("SysUserLog")));

        var indexes = await IndexNamesAsync("SysUserLog");
        Assert.Contains("IX_SysUserLog_CreateUser_CreateDate", indexes);
        Assert.Contains("IX_SysUserLog_FunctionId_CreateDate", indexes);
        Assert.DoesNotContain("IX_SysUserLog_Operator_Time", indexes);
        Assert.DoesNotContain("IX_SysUserLog_Function_Time", indexes);
    }

    // 情境：聯絡資料不屬憑證，不標記 [SensitiveData]，異動紀錄保留改前改後值（設計決策 R6）。
    // @SPEC-013 @AC-33
    [Fact(DisplayName = "Phone 與 Email 未標記 SensitiveData（設計決策 R6）")]
    public void Phone_and_email_are_not_sensitive()
    {
        foreach (var name in new[] { nameof(User.Phone), nameof(User.Email) })
        {
            var property = typeof(User).GetProperty(name)!;
            Assert.Null(property.GetCustomAttribute<SensitiveDataAttribute>());
        }
    }
}
