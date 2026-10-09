using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Data;

/// <summary>
/// Vogen 強型別主鍵由資料庫 IDENTITY 配號（ADR-010、docs/DATABASE-SCHEMA.md §4.1、DEF-012）：
/// 以 Vogen 為主鍵的表 Id 皆為 IDENTITY、同時新增不撞號；資料庫結構（外鍵、索引、說明）與 EF 模型一致。
/// 新增以 Vogen 為主鍵的資料表時加進 <see cref="Tables"/>。
/// </summary>
public class IdentityPrimaryKeyTests : IAsyncLifetime
{
    /// <summary>以 Vogen 強型別為主鍵的資料表（ADR-010）。</summary>
    public static readonly string[] Tables =
    [
        "SysTask", "SysRole", "SysRoleTask", "SysUser", "SysUserRole", "SysUserSession",
    ];

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

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();
        await using var ctx = NewContext();
        await ctx.Database.MigrateAsync(Ct);
        await SeedData.SeedAsync(ctx, new Pbkdf2PasswordHasher(), "Admin123!", Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // 情境：以 Vogen 為主鍵的表，Id 欄位都是 IDENTITY（由資料庫配號，不再由程式端算 Max+1）。
    [Fact(DisplayName = "Vogen 主鍵的資料表 Id 皆為 IDENTITY（ADR-010）")]
    public async Task All_vogen_primary_keys_are_identity()
    {
        var identityTables = await QueryAsync<string>(
            "SELECT OBJECT_NAME(object_id) FROM sys.identity_columns WHERE name = 'Id'");

        Assert.Empty(Tables.Except(identityTables));
    }

    // 情境：兩個連線在對方存檔前各自新增（模擬兩人同時新增）。程式端 Max+1 會算出同一個 Id 而撞號；
    // 由資料庫配號時兩筆都成功且 Id 不同。分別以單層實體（選單）與有子實體的聚合（角色）驗證。
    [Fact(DisplayName = "兩個連線同時新增時不撞號（ADR-010）")]
    public async Task Concurrent_inserts_do_not_collide()
    {
        await using var first = NewContext();
        await using var second = NewContext();
        var menuA = new MenuItem(MenuItemName.From("併發選單A"), null, "concurrentA");
        var menuB = new MenuItem(MenuItemName.From("併發選單B"), null, "concurrentB");
        var roleA = new Role(RoleCode.From("IDA"), RoleName.From("併發角色A"));
        var roleB = new Role(RoleCode.From("IDB"), RoleName.From("併發角色B"));

        first.AddRange(menuA, roleA);
        second.AddRange(menuB, roleB);
        await first.SaveChangesAsync(Ct);
        await second.SaveChangesAsync(Ct);

        Assert.NotEqual(menuA.Id, menuB.Id);
        Assert.NotEqual(roleA.Id, roleB.Id);
        Assert.All([menuA.Id.Value, menuB.Id.Value, roleA.Id.Value, roleB.Id.Value], id => Assert.True(id > 0));
    }

    // 情境：資料庫結構與 EF 模型一致——每個外鍵、索引、主鍵名稱都存在，表與欄位的中文說明與模型相同。
    // 手寫或重建資料表的 migration 容易漏掉約束與說明，本測試確保沒有遺漏。
    [Fact(DisplayName = "外鍵、索引、主鍵與中文說明皆與 EF 模型一致（ADR-010）")]
    public async Task Schema_matches_model()
    {
        await using var ctx = NewContext();
        var foreignKeys = (await QueryAsync<string>(
            "SELECT OBJECT_NAME(parent_object_id) + '|' + name + '|' + OBJECT_NAME(referenced_object_id) FROM sys.foreign_keys"))
            .ToHashSet();
        var indexes = (await QueryAsync<string>(
            "SELECT OBJECT_NAME(object_id) + '|' + name FROM sys.indexes WHERE name IS NOT NULL AND OBJECTPROPERTY(object_id, 'IsUserTable') = 1"))
            .ToHashSet();
        var comments = (await QueryAsync<string>(
            "SELECT OBJECT_NAME(major_id) + '|' + ISNULL(COL_NAME(major_id, minor_id), '') + '|' + CAST(value AS nvarchar(4000)) " +
            "FROM sys.extended_properties WHERE name = 'MS_Description' AND class = 1"))
            .ToHashSet();

        var missing = new List<string>();
        // 說明（comment）等設計時設定不在執行時的唯讀模型中，須取設計時模型。
        var model = ctx.GetService<IDesignTimeModel>().Model;
        foreach (var entity in model.GetEntityTypes().Where(e => e.GetTableName() is not null && Tables.Contains(e.GetTableName())))
        {
            var table = entity.GetTableName()!;
            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            missing.AddRange(entity.GetForeignKeys()
                .Select(fk => $"{table}|{fk.GetConstraintName()}|{fk.PrincipalEntityType.GetTableName()}")
                .Where(key => !foreignKeys.Contains(key)));
            missing.AddRange(entity.GetReferencingForeignKeys()
                .Select(fk => $"{fk.DeclaringEntityType.GetTableName()}|{fk.GetConstraintName()}|{table}")
                .Where(key => !foreignKeys.Contains(key)));
            missing.AddRange(entity.GetIndexes().Select(i => $"{table}|{i.GetDatabaseName()}")
                .Append($"{table}|{entity.FindPrimaryKey()!.GetName()}")
                .Where(key => !indexes.Contains(key)));
            if (entity.GetComment() is { } tableComment && !comments.Contains($"{table}||{tableComment}"))
            {
                missing.Add($"{table} 表說明");
            }

            missing.AddRange(entity.GetProperties()
                .Where(p => p.GetComment() is not null)
                .Select(p => $"{table}|{p.GetColumnName(store)}|{p.GetComment()}")
                .Where(key => !comments.Contains(key)));
        }

        Assert.Empty(missing);
    }

    private async Task<List<T>> QueryAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(_db.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        var result = new List<T>();
        while (await reader.ReadAsync(Ct))
        {
            result.Add(reader.GetFieldValue<T>(0));
        }

        return result;
    }
}
