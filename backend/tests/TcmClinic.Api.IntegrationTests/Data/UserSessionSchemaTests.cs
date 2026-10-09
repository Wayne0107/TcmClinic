using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Auditing;
using TcmClinic.Api.Infrastructure.Data.Interceptors;

namespace TcmClinic.Api.IntegrationTests.Data;

/// <summary>
/// 登入 session 表 SysUserSession（SPEC-004 AC-39，ADR-011）：欄位型別、說明、唯一索引與外鍵；
/// 每 15 分鐘輪替一次，不寫資料異動紀錄（[NotAudited]）。
/// </summary>
public class UserSessionSchemaTests : IAsyncLifetime
{
    private const string Table = "SysUserSession";

    private readonly TestDatabase _db = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public short MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }
        public string? Description { get; set; }
    }

    private sealed class IndexInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsUnique { get; set; }
    }

    private sealed class ForeignKeyInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string ReferencedTable { get; set; } = string.Empty;
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

    // 情境：14 欄的型別、長度、可空與 ADR-011 一致；主鍵為 IDENTITY；表與每欄皆有繁中說明。
    // @SPEC-004 @AC-39
    [Fact(DisplayName = "SysUserSession 欄位、型別與說明符合設計（SPEC-004 AC-39）")]
    public async Task Table_columns_match_design()
    {
        await using var ctx = NewContext();
        var cols = (await ctx.Database.SqlQueryRaw<ColumnInfo>(
            """
            SELECT c.name AS ColumnName, t.name AS TypeName, c.max_length AS MaxLength,
                   c.is_nullable AS IsNullable, c.is_identity AS IsIdentity,
                   CAST(ep.value AS nvarchar(4000)) AS Description
            FROM sys.columns c
            JOIN sys.types t ON c.user_type_id = t.user_type_id
            LEFT JOIN sys.extended_properties ep
              ON ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = 'MS_Description'
            WHERE c.object_id = OBJECT_ID({0})
            """, Table).ToListAsync(Ct)).ToDictionary(c => c.ColumnName);

        var expected = new (string Name, string Type, short Bytes, bool Nullable)[]
        {
            ("Id", "int", 4, false),
            ("PublicId", "uniqueidentifier", 16, false),
            ("UserId", "int", 4, false),
            ("TokenHash", "nvarchar", 128, false),
            ("PreviousTokenHash", "nvarchar", 128, true),
            ("ExpiresAt", "datetime", 8, false),
            ("LastRotatedAt", "datetime", 8, true),
            ("RevokedAt", "datetime", 8, true),
            ("RevokeReason", "nvarchar", 40, true),
            ("CreateDate", "datetime", 8, false),
            ("CreateUser", "nvarchar", 100, false),
            ("ModifyDate", "datetime", 8, false),
            ("ModifyUser", "nvarchar", 100, false),
            ("Removed", "bit", 1, false),
        };

        Assert.Equal(expected.Select(e => e.Name).Order(), cols.Keys.Order());
        foreach (var (name, type, bytes, nullable) in expected)
        {
            Assert.Equal(type, cols[name].TypeName);
            Assert.Equal(bytes, cols[name].MaxLength);
            Assert.Equal(nullable, cols[name].IsNullable);
            Assert.False(string.IsNullOrWhiteSpace(cols[name].Description), $"{name} 缺少說明");
        }

        Assert.True(cols["Id"].IsIdentity);
        Assert.False(string.IsNullOrWhiteSpace((await ctx.Database.SqlQueryRaw<string>(
            """
            SELECT CAST(ep.value AS nvarchar(4000)) AS Value FROM sys.extended_properties ep
            WHERE ep.major_id = OBJECT_ID({0}) AND ep.minor_id = 0 AND ep.name = 'MS_Description'
            """, Table).ToListAsync(Ct)).SingleOrDefault()));
    }

    // 情境：PublicId、TokenHash 唯一；PreviousTokenHash 有索引供重用偵測查詢；UserId 外鍵指向 SysUser。
    // @SPEC-004 @AC-39
    [Fact(DisplayName = "SysUserSession 唯一索引與外鍵符合設計（SPEC-004 AC-39）")]
    public async Task Indexes_and_foreign_key_match_design()
    {
        await using var ctx = NewContext();
        var indexes = (await ctx.Database.SqlQueryRaw<IndexInfo>(
            "SELECT name AS Name, is_unique AS IsUnique FROM sys.indexes WHERE object_id = OBJECT_ID({0}) AND name IS NOT NULL",
            Table).ToListAsync(Ct)).ToDictionary(i => i.Name);
        var foreignKeys = await ctx.Database.SqlQueryRaw<ForeignKeyInfo>(
            """
            SELECT c.name AS ColumnName, OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            WHERE fk.parent_object_id = OBJECT_ID({0})
            """, Table).ToListAsync(Ct);

        Assert.True(indexes["IX_SysUserSession_PublicId"].IsUnique);
        Assert.True(indexes["IX_SysUserSession_TokenHash"].IsUnique);
        Assert.False(indexes["IX_SysUserSession_PreviousTokenHash"].IsUnique);
        Assert.Contains("IX_SysUserSession_UserId", indexes.Keys);
        var fk = Assert.Single(foreignKeys);
        Assert.Equal(("UserId", "SysUser"), (fk.ColumnName, fk.ReferencedTable));
    }

    // 情境：session 每 15 分鐘輪替，寫入資料異動紀錄只會產生大量雜湊變更，故不在稽核範圍。
    // @SPEC-004 @AC-39
    [Fact(DisplayName = "SysUserSession 不寫資料異動紀錄（SPEC-004 AC-39，ADR-011）")]
    public void Session_is_not_audited()
    {
        Assert.False(AuditScope.IsAuditedType(typeof(UserSession)));
    }
}
