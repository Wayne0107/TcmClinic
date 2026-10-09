using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TcmClinic.Api.Infrastructure.Data;

/// <summary>
/// 設計時 DbContext 工廠，供 `dotnet ef migrations` 使用（不會實際連線）。
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // 設計時（migrations）不會實際連線；localhost loopback 用 Encrypt=False，
        // 不以 TrustServerCertificate=True 停用憑證驗證（Principle XVIII 加密，避免弱掃標記）。
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=TcmClinicApi;Trusted_Connection=True;Encrypt=False")
            .Options;

        return new AppDbContext(options);
    }
}
