using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TcmClinic.Api.Web.Configurations;
using Serilog;
using Serilog.Events;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 生產日誌格式與保留（docs/LOGGING.md §1～§3、ADR-008、DEF-014）：JSON、snake_case 必要欄位、
/// 依等級分檔並各自保留，請求日誌帶 source_ip。需 Docker Desktop 執行中。
/// </summary>
[Collection(LoggingCaptureCollection.Name)]
public class LoggingFileFormatTests(LoggingCaptureWebApplicationFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly string[] RequiredFields = ["timestamp", "level", "message", "service", "environment", "trace_id"];

    // 情境：HTTP 請求記錄（請求完成時一筆）帶 source_ip，且每筆日誌都帶 service、environment，
    // 維運才能從 log 檔追查來源並分辨是哪個服務、哪個環境寫的。
    [Fact(DisplayName = "請求日誌帶 source_ip，每筆日誌帶 service 與 environment（LOGGING §1、§3）")]
    public async Task Request_log_carries_source_ip_and_service_properties()
    {
        await factory.CreateClient().PostAsJsonAsync(
            "/auth/login", new { account = "admin", password = "WrongPass9!" }, Ct);

        var requestLog = factory.Sink.EventSnapshot().Last(e =>
            e.MessageTemplate.Text.StartsWith("HTTP ", StringComparison.Ordinal)
            && e.Properties.TryGetValue("RequestPath", out var path) && path.ToString() == "\"/auth/login\"");
        Assert.True(requestLog.Properties.ContainsKey("source_ip"));
        Assert.Equal("\"TcmClinic.Api\"", requestLog.Properties["service"].ToString());
        Assert.True(requestLog.Properties.ContainsKey("environment"));
    }

    // 情境：以生產設定寫檔時，依最低等級分成 debug／info／warning／error 四個檔：debug 只收 Debug，
    // 其餘是「該等級以上」；每行都是 JSON，最上層有 snake_case 必要欄位，其他屬性收在 properties。
    [Fact(DisplayName = "生產設定依等級分檔寫出 JSON，每行帶 snake_case 必要欄位（LOGGING §2、§3）")]
    public void Production_config_writes_json_files_split_by_level()
    {
        var dir = Directory.CreateTempSubdirectory("chb-logs-").FullName;
        try
        {
            var configuration = ProductionConfiguration(dir, minimumLevel: "Debug");
            using (var activity = new Activity("logging-test").Start())
            using (var logger = new LoggerConfiguration().ConfigureSerilog(configuration, "Production").CreateLogger())
            {
                logger.Debug("除錯 {Step}", "d");
                logger.Information("資訊 {Step}", "i");
                logger.Warning("警告 {Step}", "w");
                logger.Error(new InvalidOperationException("失敗"), "錯誤 {Step}", "e");
            }

            Assert.Equal(["d"], Steps(dir, "debug"));
            Assert.Equal(["i", "w", "e"], Steps(dir, "info"));
            Assert.Equal(["w", "e"], Steps(dir, "warning"));
            Assert.Equal(["e"], Steps(dir, "error"));

            foreach (var line in Directory.GetFiles(dir).SelectMany(File.ReadAllLines))
            {
                using var json = JsonDocument.Parse(line);
                var root = json.RootElement;
                Assert.All(RequiredFields, field => Assert.True(root.TryGetProperty(field, out _), $"缺欄位 {field}：{line}"));
                Assert.Equal("TcmClinic.Api", root.GetProperty("service").GetString());
                Assert.Equal("Production", root.GetProperty("environment").GetString());
                Assert.Equal(JsonValueKind.Object, root.GetProperty("properties").ValueKind);
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // 情境：各等級檔案以天數保留（Debug 7、Information 30、Warning 90、Error 365 天），不再以檔數限制，
    // 避免單日檔案多時錯誤紀錄提早被刪。
    [Theory(DisplayName = "生產設定各等級檔案依天數保留（LOGGING §2）")]
    [InlineData("debug", "7.00:00:00")]
    [InlineData("info", "30.00:00:00")]
    [InlineData("warning", "90.00:00:00")]
    [InlineData("error", "365.00:00:00")]
    public void Production_files_are_retained_by_days(string level, string retention)
    {
        var args = FileSinkArgs(ProductionConfiguration(Path.GetTempPath(), minimumLevel: null))
            .Single(a => Path.GetFileName(a["path"]!).StartsWith(level + "-", StringComparison.Ordinal));

        Assert.Equal(retention, args["retainedFileTimeLimit"]);
        Assert.True(string.IsNullOrEmpty(args["retainedFileCountLimit"]), "不得再以檔數限制保留");
        Assert.Equal("true", args["rollOnFileSizeLimit"], ignoreCase: true);
    }

    private IConfiguration ProductionConfiguration(string logDirectory, string? minimumLevel)
    {
        var contentRoot = factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var baseConfig = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Production.json", optional: false)
            .Build();

        // 只把 log 檔路徑導到暫存目錄，其餘設定照生產檔；必要時調低最低等級以產生 Debug 事件。
        var overrides = baseConfig.AsEnumerable()
            .Where(kv => kv.Key.EndsWith(":Args:path", StringComparison.Ordinal) && kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => (string?)Path.Combine(logDirectory, Path.GetFileName(kv.Value!)));
        if (minimumLevel is not null)
        {
            overrides["Serilog:MinimumLevel:Default"] = minimumLevel;
        }

        return new ConfigurationBuilder().AddConfiguration(baseConfig).AddInMemoryCollection(overrides).Build();
    }

    private static IEnumerable<IConfigurationSection> FileSinkArgs(IConfiguration configuration) =>
        configuration.AsEnumerable()
            .Where(kv => kv.Key.EndsWith(":Args:path", StringComparison.Ordinal))
            .Select(kv => configuration.GetSection(kv.Key[..^":path".Length]));

    private static string[] Steps(string dir, string level) =>
        Directory.GetFiles(dir, level + "-*.json")
            .SelectMany(File.ReadAllLines)
            .Select(line =>
            {
                using var json = JsonDocument.Parse(line);
                return json.RootElement.GetProperty("properties").GetProperty("Step").GetString()!;
            })
            .ToArray();
}
