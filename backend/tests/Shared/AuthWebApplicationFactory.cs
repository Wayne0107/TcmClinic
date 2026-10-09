using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TcmClinic.Api.Tests.Shared;

/// <summary>
/// 使用應用程式真實驗證 scheme（2026-10-08 起為 JWT Bearer＋refresh Cookie，ADR-011）的測試工廠（不注入 TestAuthHandler），
/// 功能與驗收測試共用：用以驗證未登入 401、登入簽發 Cookie、R8 每請求重驗，以及 005 種子 admin 登入後即具管理權。
/// 種子（SeedData）已建立三個管理選單並授予 admin 可見性，故無需額外 bootstrap。需 Docker Desktop。
/// </summary>
public class AuthWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// 測試用簽章金鑰（Base64）：由固定文字於執行時編碼，不是真實秘密。以設定 <c>Jwt:SigningKey</c> 帶入，
    /// 讓測試能自行產生過期、簽章不符等 token（AC-36）。
    /// </summary>
    public static readonly string SigningKey =
        Convert.ToBase64String(Encoding.UTF8.GetBytes("TcmClinic.Api functional tests signing key, not a secret"));

    private readonly TestDatabase _dbContainer = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        // 登入限速（FR-006b）放寬：TestServer 連線沒有 IP，所有登入會算成同一來源；限速本身由 LoginRateLimitTests 驗證。
        builder.UseSetting("LoginRateLimit:PermitLimit", "100000");
        // 不覆寫驗證 scheme → 使用 AddAuthConfigs 註冊的 JWT Bearer 驗證（SPEC-004，ADR-011）。
        // 固定簽章金鑰，讓測試能自行產生過期、簽章不符等 token（AC-36）。
        builder.UseSetting("Jwt:SigningKey", SigningKey);
    }

    public async ValueTask InitializeAsync() => await _dbContainer.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
