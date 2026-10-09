using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace TcmClinic.Api.Tests.Shared;

/// <summary>
/// 測試用資料庫：整個測試程序共用一個 SQL Server 容器，每個實例建立一個專屬資料庫並於結束時刪除。
/// 隔離程度與「每個測試一個容器」相同（每個實例都拿到全新的空資料庫），但省去每次啟動容器的時間。
/// 介面與 <see cref="MsSqlContainer"/> 用到的部分一致：<see cref="StartAsync"/>、<see cref="GetConnectionString"/>、<see cref="DisposeAsync"/>。
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private static readonly MsSqlContainer Container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    // 第一個呼叫者啟動容器，其餘等待同一個工作；容器於測試程序結束時由 Testcontainers 清除。
    private static readonly Lazy<Task> ContainerStarted = new(() => Container.StartAsync());

    private readonly string _databaseName = $"Test_{Guid.NewGuid():N}";
    private string? _connectionString;

    /// <summary>確保共用容器已啟動，並建立本實例專屬的空資料庫。</summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await ContainerStarted.Value;
        await ExecuteOnMasterAsync($"CREATE DATABASE [{_databaseName}]", cancellationToken);
        _connectionString = new SqlConnectionStringBuilder(Container.GetConnectionString())
        {
            InitialCatalog = _databaseName,
        }.ConnectionString;
    }

    /// <summary>本實例專屬資料庫的連線字串；須先呼叫 <see cref="StartAsync"/>。</summary>
    public string GetConnectionString() =>
        _connectionString ?? throw new InvalidOperationException("尚未呼叫 StartAsync，資料庫還不存在。");

    /// <summary>刪除本實例的資料庫；先清掉連線池並中斷殘留連線，避免 DROP 被占用中的連線擋下。</summary>
    public async ValueTask DisposeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        SqlConnection.ClearPool(new SqlConnection(_connectionString));
        await ExecuteOnMasterAsync(
            $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]",
            CancellationToken.None);
        _connectionString = null;
    }

    private static async Task ExecuteOnMasterAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(Container.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
