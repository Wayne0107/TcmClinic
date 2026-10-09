using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace TcmClinic.Api.Tests.Shared;

/// <summary>
/// 功能與驗收測試共用的工廠：在共用的 SQL Server 2022 容器中建立專屬資料庫（<see cref="TestDatabase"/>），注入連線字串，
/// 並以測試 scheme 認證為超級管理員。啟動時 Program 會自動套用 migration 並種入資料。
/// 需 Docker Desktop 執行中。
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabase _dbContainer = new();

    public string ConnectionString => _dbContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development → 觸發 Program 的 migrate + seed；連線指向測試容器。
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        // 登入限速（FR-006b）放寬：TestServer 連線沒有 IP，所有登入會算成同一來源；限速本身由 LoginRateLimitTests 驗證。
        builder.UseSetting("LoginRateLimit:PermitLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public async ValueTask InitializeAsync() => await _dbContainer.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
