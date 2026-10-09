using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 日誌等級功能測試（docs/LOGGING.md §3：log level 語意化）。EF Core 每條 SQL 指令以 Information 記錄，
/// 若不壓低，正式 log 檔會被 SQL 文字灌滿；指令失敗屬 Error，仍須保留。需 Docker Desktop 執行中。
/// </summary>
[Collection(LoggingCaptureCollection.Name)]
public class LoggingLevelTests(LoggingCaptureWebApplicationFactory factory)
{
    private const string EfCommandCategory = "Microsoft.EntityFrameworkCore.Database.Command";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境：一次會查資料庫的登入請求，日誌 MUST NOT 含 EF Core 成功執行 SQL 的 Information 事件。
    [Fact(DisplayName = "請求內不記錄 EF Core 成功執行 SQL 的 Information 日誌（LOGGING §3）")]
    public async Task Ef_core_sql_commands_not_logged_at_information()
    {
        var traceId = ActivityTraceId.CreateRandom();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new { account = "admin", password = "WrongPass9!" }),
        };
        request.Headers.Add(
            "traceparent", $"00-{traceId.ToHexString()}-{ActivitySpanId.CreateRandom().ToHexString()}-01");

        await factory.CreateClient().SendAsync(request, Ct);

        var traced = factory.Sink.EventSnapshot().Where(e => e.TraceId == traceId).ToList();

        // 前提健檢：該請求確有進入用例（登入會查 SysUser），避免以下斷言因沒捕捉到事件而恆真。
        Assert.Contains(traced, e => e.MessageTemplate.Text == "處理中 {MessageType}");
        Assert.DoesNotContain(traced, e =>
            e.Level < LogEventLevel.Warning && SourceContextOf(e) == EfCommandCategory);
    }

    // 情境：各環境設定 MUST 把 EF Core SQL 指令類別壓到 Warning 以上（正式環境的 log 檔同樣不灌 SQL）。
    [Theory(DisplayName = "各環境設定把 EF Core SQL 指令日誌壓到 Warning（LOGGING §3）")]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Ef_core_command_category_overridden_to_warning(string environment)
    {
        var contentRoot = factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var level = configuration[$"Serilog:MinimumLevel:Override:{EfCommandCategory}"];

        Assert.True(
            Enum.TryParse<LogEventLevel>(level, out var parsed) && parsed >= LogEventLevel.Warning,
            $"{environment} 未把 {EfCommandCategory} 壓到 Warning 以上（目前：{level ?? "未設定"}）");
    }

    private static string? SourceContextOf(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("SourceContext", out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : null;
}
