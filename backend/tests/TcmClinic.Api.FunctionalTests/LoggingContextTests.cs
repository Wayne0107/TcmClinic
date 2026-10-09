using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 日誌請求上下文功能測試（docs/LOGGING.md §3：日誌帶 trace id，使單一請求可跨 HTTP、Mediator、基礎設施邊界追蹤）。
/// 需 Docker Desktop 執行中。
/// </summary>
[Collection(LoggingCaptureCollection.Name)]
public class LoggingContextTests(LoggingCaptureWebApplicationFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境：以已知 traceparent 送出登入請求，該請求內的 Mediator LoggingBehavior 日誌與 HTTP 請求記錄
    // MUST 帶同一 TraceId，且 LoggingBehavior 日誌帶 ASP.NET Core 請求範圍的 RequestPath，
    // 維運才能以 TraceId 撈出同一請求的全部日誌。
    [Fact(DisplayName = "同一請求的 Mediator 日誌與 HTTP 請求記錄共用 TraceId 並帶請求路徑（LOGGING §3）")]
    public async Task Logs_within_request_share_trace_context()
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
        Assert.Contains(traced, e =>
            e.MessageTemplate.Text == "處理中 {MessageType}"
            && ScalarOf(e, "MessageType") == "LoginCommand"
            && ScalarOf(e, "RequestPath") == "/auth/login");
        Assert.Contains(traced, e =>
            e.MessageTemplate.Text.StartsWith("HTTP ", StringComparison.Ordinal)
            && ScalarOf(e, "RequestPath") == "/auth/login");
    }

    // 情境：文字格式的 sink（開發與正式的 Console）輸出格式 MUST 印出 TraceId，否則上下文雖在 LogEvent 上，
    // 落到主控台時仍看不到、無法搜尋。正式環境的 JSON log 檔以 trace_id 欄位呈現，見 LoggingFileFormatTests。
    [Theory(DisplayName = "各環境文字格式的日誌 sink 都印出 TraceId（LOGGING §3）")]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Every_sink_output_template_renders_trace_id(string environment)
    {
        var contentRoot = factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();
        var sinks = configuration.GetSection("Serilog:WriteTo").GetChildren()
            .Where(sink => sink["Args:outputTemplate"] is not null)
            .ToList();

        var traceId = ActivityTraceId.CreateRandom();
        var logEvent = new LogEvent(
            DateTimeOffset.Now, LogEventLevel.Information, exception: null,
            new MessageTemplateParser().Parse("處理中 {MessageType}"),
            [new LogEventProperty("MessageType", new ScalarValue("LoginCommand"))],
            traceId, ActivitySpanId.CreateRandom());

        // 前提健檢：確有設定文字格式的 sink，避免以下迴圈因空集合而恆真。
        Assert.NotEmpty(sinks);
        foreach (var sink in sinks)
        {
            var template = sink["Args:outputTemplate"];
            Assert.False(string.IsNullOrEmpty(template), $"sink {sink["Name"]} 未設定 outputTemplate");

            using var writer = new StringWriter();
            new MessageTemplateTextFormatter(template).Format(logEvent, writer);
            Assert.Contains(traceId.ToHexString(), writer.ToString(), StringComparison.Ordinal);
        }
    }

    private static string? ScalarOf(LogEvent logEvent, string propertyName) =>
        logEvent.Properties.TryGetValue(propertyName, out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : null;
}
