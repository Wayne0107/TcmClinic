using Serilog;
using Serilog.AspNetCore;

namespace TcmClinic.Api.Web.Configurations;

public static class LoggerConfigs
{
    /// <summary>
    /// [維運提示] 結構化日誌（Serilog，Principle XVII）。
    /// sink、最低等級與輸出格式一律由設定驅動（ReadFrom.Configuration）——appsettings.json 為 Console，
    /// appsettings.Production.json 追加依等級分檔的 JSON File（debug／info／warning／error，各自以天數保留，
    /// 每日＋單檔 100 MB 雙觸發輪替；正式/IIS 無附掛主控台時的持久化紀錄來源，ADR-008）。
    /// 另讀取 DI 註冊的 sink（ReadFrom.Services），供功能測試掛入捕捉 sink 而不必改寫本設定。
    /// 事件同時轉送給 DI 註冊的 ILoggerProvider（writeToProviders），使 ServiceDefaults 的 OpenTelemetry
    /// 日誌提供者取得日誌、經 OTLP 送進 Aspire Dashboard 的結構化日誌；預設的 Console／Debug／EventLog／
    /// EventSource 提供者先行清除，避免與 Serilog 的主控台輸出重複，或在 IIS 上寫進 Windows 事件檢視器。
    /// 本方法須在 AddServiceDefaults 之前呼叫，否則會一併清掉 OpenTelemetry 提供者。
    /// 請求延遲由 UseSerilogRequestLogging 記錄（Principle XIV）；密碼等機密 MUST NOT 入日誌
    /// （LoggingBehavior 僅記訊息型別名、請求記錄不含 body；由 LoggingRedactionTests 護欄）。
    /// 監控關鍵：正式環境 log 檔於站台目錄 logs/；IIS 應用程式集區身分須具該目錄寫入權限，否則 sink 靜默失敗。
    /// </summary>
    public static WebApplicationBuilder AddLoggerConfigs(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog(
            (context, services, configuration) => configuration
                .ConfigureSerilog(context.Configuration, context.HostingEnvironment.EnvironmentName)
                .ReadFrom.Services(services),
            writeToProviders: true);
        return builder;
    }

    /// <summary>
    /// 讀取設定並加上每筆日誌必帶的 <c>service</c>、<c>environment</c>（ADR-008）。
    /// 生產環境的 JSON 格式與依等級分檔保留寫在 appsettings.Production.json，本方法只補設定檔無法表達的環境名稱。
    /// </summary>
    public static LoggerConfiguration ConfigureSerilog(
        this LoggerConfiguration logger, IConfiguration configuration, string environmentName) =>
        logger
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("service", ServiceName)
            .Enrich.WithProperty("environment", environmentName);

    /// <summary>請求完成時記一筆（UseSerilogRequestLogging），加記來源 IP（docs/LOGGING.md §1）。</summary>
    public static void ConfigureRequestLogging(RequestLoggingOptions options) =>
        options.EnrichDiagnosticContext = (diagnostics, httpContext) =>
            diagnostics.Set("source_ip", httpContext.Connection.RemoteIpAddress?.ToString());

    private const string ServiceName = "TcmClinic.Api";
}
