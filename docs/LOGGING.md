# 日誌與診斷規範（Logging & Diagnostics）

> 版本 1.3.0｜最後更新 2026-10-08

> 本檔為憲章原則 I（安全性——秘密/個資遮罩）與 VI（可觀測效能）的 **How**。
> 每個部署環境都必須產出持久、結構化、可查詢的日誌。日誌是系統在生產環境（如客戶自建 IIS）失常時的主要診斷紀錄。

## 1. 結構化日誌（MUST）

- 所有應用程式日誌必須經 Serilog，用 message template 形式（`_logger.LogInformation("處理中 {MessageType}", name)`），保持結構化，不要預先內插字串。
- HTTP 請求必須經 `UseSerilogRequestLogging` 記錄（method、path、status、elapsed、source_ip）。請求完成時記一筆即可，不另在收到請求時先記（2026-10-08 裁決）。`source_ip` 以 `EnrichDiagnosticContext` 從 `HttpContext.Connection.RemoteIpAddress` 帶入；部署在反向代理後方時須設定 Forwarded Headers 才取得到真實來源。實作見 `LoggerConfigs.ConfigureRequestLogging`。
- Mediator 訊息處理經 pipeline `LoggingBehavior` 記錄；應用程式碼透過注入的 `ILogger<T>` 發出領域日誌。

## 2. 依環境選擇 Sink（MUST）

- **開發**：Console sink 即可（Aspire dashboard / 終端機）。Serilog 以 `writeToProviders: true` 把事件轉送給 ServiceDefaults 註冊的 OpenTelemetry 日誌提供者，經 OTLP 送進 Aspire Dashboard 的「結構化日誌」頁，可依 TraceId、等級、屬性篩選。
  - 預設的 Console／Debug／EventLog／EventSource 提供者已於 `LoggerConfigs` 清除（避免重複輸出），故 `AddLoggerConfigs` 必須在 `AddServiceDefaults` 之前呼叫。
  - 轉送時訊息由 Serilog 渲染，字串值會帶引號（如 `處理中 "LoginCommand"`），屬正常現象。
- **生產／自建（IIS 或任何非開發部署）**：日誌必須寫入持久 sink——rolling File sink，因為託管程序無附掛 console，stdout 非可靠紀錄。
  - File sink 必須同時依時間與大小輪替：每日輪替（`RollingInterval.Day`）＋單檔大小上限（`fileSizeLimitBytes`，預設 100 MB）＋`rollOnFileSizeLimit: true`。只設時間輪替時，Serilog 單檔達 1 GB 預設上限後會**靜默停止寫入**。
  - 依等級保留期限（2026-10-08 裁決，對齊 UDS）：`Debug` 7 天、`Information` 30 天、`Warning` 90 天、`Error` 以上 1 年。做法是依最低等級分開寫檔（子 logger 以 `MinimumLevel` 或 filter 區分），各自以 `retainedFileTimeLimit` 控制天數；大小輪替照上一點保留。磁碟容量由部署端監控。
  - 檔案：`logs/debug-YYYYMMDD.json`（只收 Verbose、Debug）、`info-`（Information 以上）、`warning-`（Warning 以上）、`error-`（Error 以上），設定在 `appsettings.Production.json`，決策見 [ADR-008](../specs/其他/架構決策/ADR-008-production-json-logs-by-level.md)。
  - 可另加 Seq、OTLP collector、資料庫等 sink，但本機持久紀錄必須作為基線存在。
- Sink 與最低層級選擇必須由設定驅動（`ReadFrom.Configuration`），讓維運可透過 `appsettings.{Environment}.json` / 環境變數調整，無需改碼或重部署。
- 部署必須確保日誌目錄對託管身分（如 IIS app pool identity）可寫；靜默寫入失敗的 sink = 缺陷。

## 3. 內容與 enrichment（MUST）

- 日誌必須帶 trace id，讓單一請求可跨 HTTP、Mediator、基礎設施邊界追蹤（對齊 OpenTelemetry trace context）。
  - Serilog 會從 `Activity.Current` 自動把 `TraceId`／`SpanId` 帶進每筆 LogEvent，`ILogger<T>` 的 scope 屬性（如 ASP.NET Core 的 `RequestPath`、`RequestId`）也會一併帶入，不需另設 enricher。
  - 文字格式的 sink（開發環境主控台）必須在 `outputTemplate` 印出 `{TraceId}`，否則 trace id 雖在事件上，落到主控台時仍看不到。查同一請求的日誌即以該 TraceId 搜尋。
  - 外部呼叫端若帶 W3C `traceparent` header，TraceId 會沿用呼叫端的值，可跨系統串接。
- 日誌訊息用繁體中文；需要被搜尋或監控的事件以英文方括號 tag 開頭（如 `[SYSTEM_PARAMETER] 參數缺列：…`、`[LOGIN_LOCKED]`）。例外訊息同樣用繁中。
- 生產環境的 log 檔用 JSON（一行一筆），欄位名稱用 snake_case，每筆必帶 `timestamp`、`level`、`message`、`service`、`environment`、`trace_id`，其餘屬性一併輸出（2026-10-08 裁決，對齊 UDS）。`service` 固定為 `TcmClinic.Api`，`environment` 取 ASP.NET Core 環境名稱。實作建議用 Serilog.Expressions 的 `ExpressionTemplate` 自訂欄位名稱（新增套件依 `docs/DEPENDENCIES.md` 登記）。開發環境主控台維持文字格式。
  - JSON 格式以 `ExpressionTemplate` 設定在 `appsettings.Production.json`；`service`、`environment` 由 `LoggerConfigs.ConfigureSerilog` 加入。最上層欄位另有 `span_id`、有例外時的 `exception`，其餘事件屬性收在 `properties` 物件內。
- 必記事件：認證失敗以 `Warning` 記錄（帳號、來源 IP；**不記密碼**），帳號鎖定記 `[LOGIN_LOCKED]`；登入限速擋下時記 `[LOGIN_RATE_LIMITED]`（來源 IP，已實作，SPEC-004 FR-006b）。認證失敗與鎖定兩項目前尚未實作，與認證日誌 `SysAuthLog` 一併處理（specs/README.md 延後事項 DEF-005）。
- 日誌**不得**含秘密或敏感資料——密碼、雜湊、token、cookie、完整連線字串、個資（PII）必須排除或遮罩。記錄這些 = 缺陷，須於審查中擋下（見 `docs/SECURE-CODING.md` §7）。
- log level 語意化：
  - `Information`：正常生命週期
  - `Warning`：可復原異常
  - `Error`：已處理失敗（附例外物件）
  - `Fatal`：終止程序等級
  - 預期的 `Result` 失敗（`NotFound`/`Invalid`）**不得**記為 `Error`。
- EF Core 每條成功執行的 SQL 指令以 `Information` 記錄，各環境設定必須把 `Microsoft.EntityFrameworkCore.Database.Command` 壓到 `Warning`，避免 log 檔被 SQL 文字灌滿；指令失敗屬 `Error`，仍會記錄。排查 SQL 時再於該環境暫時調回 `Information`。

## 4. 設定位置

- Serilog 設定集中於 `Configurations/LoggerConfigs.cs`（見 `docs/CODING-STANDARDS.md` §6）。
- 斷言日誌內容的功能測試以 DI 註冊捕捉用 `ILogEventSink`（`LoggerConfigs` 經 `ReadFrom.Services` 納入），不另改寫日誌設定以反映正式行為，並歸入 `LoggingCapture` 測試集合獨占執行：`UseSerilog` 會改寫全域 `Log.Logger`，與其他測試主機平行時事件會寫進別的主機的 sink。
- Sinks 套件（`Serilog.Sinks.Console`、`Serilog.Sinks.File`）由 Central Package Management 管理版本，並記錄於 `THIRD-PARTY-NOTICES.md`。
