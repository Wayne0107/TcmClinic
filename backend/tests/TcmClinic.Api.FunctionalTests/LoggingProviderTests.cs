using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// [維運提示] 捕捉用 Microsoft.Extensions.Logging 提供者：收集轉送到 ILoggerProvider 的格式化訊息，
/// 代表 OpenTelemetry 日誌提供者（Aspire Dashboard 結構化日誌來源）能收到的內容。僅測試用。
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<string> _messages = new();
    private readonly object _gate = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public IReadOnlyList<string> Snapshot()
    {
        lock (_gate)
        {
            return _messages.ToArray();
        }
    }

    public void Dispose()
    {
    }

    private void Add(string message)
    {
        lock (_gate)
        {
            _messages.Add(message);
        }
    }

    private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => provider.Add(formatter(state, exception));
    }
}

/// <summary>
/// 日誌轉送功能測試：Serilog 須把事件轉送給 DI 註冊的 ILoggerProvider（含 ServiceDefaults 的 OpenTelemetry
/// 日誌提供者），Aspire Dashboard 的結構化日誌才有資料；且不得殘留預設的 Console 等提供者造成重複輸出。
/// 需 Docker Desktop 執行中。
/// </summary>
[Collection(LoggingCaptureCollection.Name)]
public class LoggingProviderTests(LoggingCaptureWebApplicationFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境：登入請求的 Mediator 日誌 MUST 轉送到 DI 註冊的 ILoggerProvider（OpenTelemetry 提供者即經此路徑取得日誌）。
    [Fact(DisplayName = "應用程式日誌轉送給 DI 註冊的日誌提供者（Aspire Dashboard 結構化日誌來源）")]
    public async Task Logs_forwarded_to_registered_logger_providers()
    {
        await factory.CreateClient().PostAsJsonAsync(
            "/auth/login", new { account = "admin", password = "WrongPass9!" }, Ct);

        // Serilog 轉送時以自身格式渲染訊息，字串值會加上引號（處理中 "LoginCommand"），故不比對完整字串。
        Assert.Contains(factory.Provider.Snapshot(), m =>
            m.StartsWith("處理中 ", StringComparison.Ordinal) && m.Contains("LoginCommand", StringComparison.Ordinal));
    }

    // 情境：OpenTelemetry 日誌提供者 MUST 已註冊；預設的 Console／Debug／EventLog／EventSource 提供者
    // MUST 已清除——Serilog 已負責主控台與檔案輸出，殘留會使每筆日誌重複印出或寫進 Windows 事件檢視器。
    [Fact(DisplayName = "已註冊 OpenTelemetry 日誌提供者，且不殘留預設的 Console 等提供者")]
    public void Only_open_telemetry_and_test_providers_registered()
    {
        var providers = factory.Services.GetServices<ILoggerProvider>()
            .Select(p => p.GetType().Name)
            .ToList();

        Assert.Contains("OpenTelemetryLoggerProvider", providers);
        Assert.DoesNotContain(providers, name => name is
            "ConsoleLoggerProvider" or "DebugLoggerProvider" or "EventLogLoggerProvider" or "EventSourceLoggerProvider");
    }
}
