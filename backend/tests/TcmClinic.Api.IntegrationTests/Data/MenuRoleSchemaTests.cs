using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;

namespace TcmClinic.Api.IntegrationTests.Data;

/// <summary>
/// 2026-09-30 資料表規範對齊綱要（後台選單與角色權限管理 設計決策 R8、使用者管理與登入 設計決策 R9）：
/// 角色／選單／角色可見選單／使用者角色四張系統表改為單數表名，選單父子關聯改為可空的自參考外鍵
/// （DATABASE-SCHEMA §5、§8）。
/// </summary>
public class MenuRoleSchemaTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>sys.columns 查詢結果。</summary>
    private sealed class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
    }

    /// <summary>sys.foreign_keys 查詢結果。</summary>
    private sealed class ForeignKeyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string ReferencedTable { get; set; } = string.Empty;
        public string DeleteAction { get; set; } = string.Empty;
    }

    /// <summary>sys.indexes 查詢結果。</summary>
    private sealed class IndexInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsUnique { get; set; }
        public string? FilterDefinition { get; set; }
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

    private async Task<bool> TableExistsAsync(string table)
    {
        await using var ctx = NewContext();
        var count = await ctx.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = {0}", table).SingleAsync(Ct);
        return count > 0;
    }

    private async Task<Dictionary<string, ColumnInfo>> ColumnsAsync(string table)
    {
        await using var ctx = NewContext();
        var rows = await ctx.Database.SqlQueryRaw<ColumnInfo>(
            """
            SELECT c.name AS ColumnName, c.is_nullable AS IsNullable
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID({0})
            """, table).ToListAsync(Ct);
        return rows.ToDictionary(r => r.ColumnName);
    }

    private async Task<List<ForeignKeyInfo>> ForeignKeysAsync(string table)
    {
        await using var ctx = NewContext();
        return await ctx.Database.SqlQueryRaw<ForeignKeyInfo>(
            """
            SELECT fk.name AS Name, c.name AS ColumnName,
                   OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
                   fk.delete_referential_action_desc AS DeleteAction
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            WHERE fk.parent_object_id = OBJECT_ID({0})
            """, table).ToListAsync(Ct);
    }

    private async Task<Dictionary<string, IndexInfo>> IndexesAsync(string table)
    {
        await using var ctx = NewContext();
        var rows = await ctx.Database.SqlQueryRaw<IndexInfo>(
            """
            SELECT name AS Name, is_unique AS IsUnique, filter_definition AS FilterDefinition
            FROM sys.indexes
            WHERE object_id = OBJECT_ID({0}) AND name IS NOT NULL
            """, table).ToListAsync(Ct);
        return rows.ToDictionary(r => r.Name);
    }

    // 情境（DATABASE-SCHEMA §8）：四張系統表改為單數表名，複數舊表名已不存在。
    // @SPEC-002 @AC-21
    // @SPEC-004 @AC-29
    [Theory(DisplayName = "系統表改為單數表名，複數舊表名已不存在（後台選單 設計決策 R8、使用者管理 設計決策 R9）")]
    [InlineData("SysRole", "SysRoles")]
    [InlineData("SysTask", "SysTasks")]
    [InlineData("SysRoleTask", "SysRoleTasks")]
    [InlineData("SysUserRole", "SysUserRoles")]
    public async Task System_tables_use_singular_names(string table, string oldTable)
    {
        Assert.True(await TableExistsAsync(table), $"{table} 應存在");
        Assert.False(await TableExistsAsync(oldTable), $"{oldTable} 應已改名");
    }

    // 情境（DATABASE-SCHEMA §5，FR-020）：選單父子關聯由資料庫外鍵保證——ParentId 可空（NULL＝頂層），
    // 外鍵指向 SysTask.Id 且不連動刪除；同層名稱唯一索引仍篩選未刪除列（SQL Server 將多個 NULL 視為相同值，頂層名稱亦唯一）。
    // @SPEC-002 @AC-18
    [Fact(DisplayName = "選單 ParentId 為可空自參考外鍵且不連動刪除，同層名稱唯一索引篩選未刪除列（FR-020、設計決策 R8）")]
    public async Task SysTask_parent_is_nullable_self_reference()
    {
        var cols = await ColumnsAsync("SysTask");
        Assert.True(cols["ParentId"].IsNullable);

        var fk = Assert.Single(await ForeignKeysAsync("SysTask"));
        Assert.Equal("FK_SysTask_SysTask_ParentId", fk.Name);
        Assert.Equal("ParentId", fk.ColumnName);
        Assert.Equal("SysTask", fk.ReferencedTable);
        Assert.Equal("NO_ACTION", fk.DeleteAction);

        var indexes = await IndexesAsync("SysTask");
        var unique = indexes["IX_SysTask_ParentId_Name"];
        Assert.True(unique.IsUnique);
        Assert.Equal("([Removed]=(0))", unique.FilterDefinition);
        Assert.Contains("PK_SysTask", indexes.Keys);
    }

    // 情境：同層名稱唯一在頂層（ParentId 為 NULL）同樣成立——第二筆同名頂層選單應被唯一索引擋下。
    // @SPEC-002 @AC-16
    [Fact(DisplayName = "頂層選單名稱唯一：同名頂層選單無法重複寫入（FR-007、設計決策 R8）")]
    public async Task Top_level_menu_names_are_unique()
    {
        await using var ctx = NewContext();
        const string insert =
            """
            INSERT INTO [SysTask] ([PublicId], [ParentId], [Route], [Name], [Icon], [DisplayOrder], [IsEnabled],
                                   [CreateDate], [CreateUser], [ModifyDate], [ModifyUser], [Removed])
            VALUES (NEWID(), NULL, NULL, N'頂層重複名稱', NULL, 0, 1, GETDATE(), 'test', GETDATE(), 'test', 0)
            """;
        await ctx.Database.ExecuteSqlRawAsync(insert, Ct);

        await Assert.ThrowsAnyAsync<Exception>(() => ctx.Database.ExecuteSqlRawAsync(insert, Ct));
    }

    // 情境：角色可見選單與使用者角色兩張關聯表的外鍵改指向單數表名，刪除行為不變。
    // @SPEC-002 @AC-21
    // @SPEC-004 @AC-29
    [Fact(DisplayName = "角色可見選單與使用者角色的外鍵指向單數表名且刪除行為不變（設計決策 R8、R9）")]
    public async Task Join_tables_reference_singular_tables()
    {
        var roleTask = await ForeignKeysAsync("SysRoleTask");
        Assert.Contains(roleTask, f => f.ColumnName == "SysRoleId" && f.ReferencedTable == "SysRole" && f.DeleteAction == "CASCADE");
        Assert.Contains(roleTask, f => f.ColumnName == "SysTaskId" && f.ReferencedTable == "SysTask" && f.DeleteAction == "CASCADE");

        var userRole = await ForeignKeysAsync("SysUserRole");
        Assert.Contains(userRole, f => f.ColumnName == "UserId" && f.ReferencedTable == "SysUser" && f.DeleteAction == "CASCADE");
        Assert.Contains(userRole, f => f.ColumnName == "RoleId" && f.ReferencedTable == "SysRole" && f.DeleteAction == "NO_ACTION");

        var userRoleIndexes = await IndexesAsync("SysUserRole");
        Assert.Equal("([Removed]=(0))", userRoleIndexes["IX_SysUserRole_UserId_RoleId"].FilterDefinition);
        Assert.Contains("PK_SysUserRole", userRoleIndexes.Keys);
        Assert.Contains("PK_SysRole", (await IndexesAsync("SysRole")).Keys);
        Assert.Contains("PK_SysRoleTask", (await IndexesAsync("SysRoleTask")).Keys);
    }
}
