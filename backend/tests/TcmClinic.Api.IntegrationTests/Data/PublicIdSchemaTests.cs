using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Data;

/// <summary>
/// 對外識別碼（SPEC-064 AC-5）：對外公開的表有 PublicId（uniqueidentifier、NOT NULL、唯一索引、中文說明），
/// 新建實體的 PublicId 為 UUIDv7；內部主鍵仍為 IDENTITY（ADR-010）。
/// </summary>
public class PublicIdSchemaTests : IAsyncLifetime
{
    public static readonly string[] Tables = ["SysUser", "SysRole", "SysTask", "SysUserSession"];

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

    // 情境：每張表的 PublicId 為 NOT NULL 的 uniqueidentifier，有唯一索引與中文說明；主鍵仍為 IDENTITY。
    // @SPEC-064 @AC-5
    [Theory(DisplayName = "PublicId 欄位為 NOT NULL uniqueidentifier、唯一索引、有說明，主鍵仍為 IDENTITY（SPEC-064 AC-5）")]
    [MemberData(nameof(TableData))]
    public async Task Public_id_column_is_unique_and_documented(string table)
    {
        var column = await QuerySingleAsync<string>($"""
            SELECT ty.name + '|' + CAST(c.is_nullable AS varchar(1)) + '|' + CAST(ISNULL(ep.value, '') AS nvarchar(400))
            FROM sys.columns c
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            LEFT JOIN sys.extended_properties ep ON ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = 'MS_Description'
            WHERE c.object_id = OBJECT_ID('{table}') AND c.name = 'PublicId'
            """);
        var parts = column.Split('|');
        Assert.Equal("uniqueidentifier", parts[0]);
        Assert.Equal("0", parts[1]);
        Assert.Contains("UUIDv7", parts[2], StringComparison.Ordinal);

        Assert.Equal(1, await QuerySingleAsync<int>($"""
            SELECT COUNT(*) FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            WHERE i.object_id = OBJECT_ID('{table}') AND i.name = 'IX_{table}_PublicId' AND i.is_unique = 1
              AND COL_NAME(ic.object_id, ic.column_id) = 'PublicId'
            """));
        Assert.Equal(1, await QuerySingleAsync<int>(
            $"SELECT COUNT(*) FROM sys.identity_columns WHERE object_id = OBJECT_ID('{table}') AND name = 'Id'"));
    }

    // 情境：新建實體於建構時產生 UUIDv7 的 PublicId，存檔後讀回不變。
    // @SPEC-064 @AC-5
    [Fact(DisplayName = "新建實體的 PublicId 為 UUIDv7 且存檔後不變（SPEC-064 AC-5）")]
    public async Task New_entity_gets_uuid_v7_public_id()
    {
        var role = new Role(RoleCode.From("PID1"), RoleName.From("對外識別碼測試"));
        Assert.Equal(7, role.PublicId.Value.Version);

        await using (var ctx = NewContext())
        {
            ctx.Roles.Add(role);
            await ctx.SaveChangesAsync(Ct);
        }

        await using var verify = NewContext();
        var stored = await verify.Roles.AsNoTracking().SingleAsync(r => r.Id == role.Id, Ct);
        Assert.Equal(role.PublicId, stored.PublicId);
    }

    public static TheoryData<string> TableData() => new(Tables);

    private async Task<T> QuerySingleAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(_db.GetConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new SqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync(Ct);
        Assert.NotNull(value);
        return (T)value;
    }
}
