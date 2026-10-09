# ADR-008 生產日誌改 JSON，並依等級分檔保留

| 項目 | 內容 |
|------|------|
| 狀態 | Accepted |
| 日期 | 2026-10-08 |
| 決策者 | Wayne（維護者） |
| 出處 | 開發規範裁決 2026-10-08（PRJ-2026-0008）第 12、13、53 題；specs/README.md 延後事項 DEF-014 |
| 取代 | `docs/LOGGING.md` 1.0 版「生產環境維持文字格式、以 `retainedFileCountLimit`（31 個檔）保留」 |

> 📦 本 ADR 隨範本沿用自源專案（CHB），決策本身仍適用於範本。背景、影響與相關連結中提到的舊系統、分行、企業、開狀申請、系統參數等屬源專案業務，相關規格、資料表與 migration 範本已移除；以骨架程式碼與測試為準。

## 背景（Context）

生產環境（客戶自建 IIS）只有一個文字格式的 log 檔（`logs/log-.txt`），每日加 100 MB 輪替，保留 31 個檔。當初的理由是客戶環境以人工閱讀 log 檔為主。

逐條比對 UDS 標準後，有三處不同：

- UDS 要求生產日誌用 JSON，必帶 `timestamp`、`level`、`message`、`service`、`environment`，欄位名稱用 snake_case（`logging.ai.yaml` 的 `structured-format`，必要）。
- UDS 建議依等級設定保留期限：Debug 7 天、Information 30 天、Warning 90 天、Error 1 年。目前只按檔數保留，單日檔案一多，錯誤紀錄可能不到一個月就被刪掉。
- UDS 要求請求日誌記來源 IP；目前的請求日誌沒有 `source_ip`。

2026-10-08 使用者裁決三項都改採 UDS，請求日誌維持「請求完成時記一筆」。

## 考慮過的選項

1. **Serilog.Expressions 的 `ExpressionTemplate` 自訂 JSON**：可以完全掌控欄位名稱，最上層全用 snake_case；需新增 Serilog.Expressions 套件（Apache-2.0）。
2. **Serilog 內建 `CompactJsonFormatter`**：不需新套件，但欄位名稱固定為 `@t`、`@l`、`@mt` 等，不符合 UDS 的欄位命名。
3. **維持文字格式**：不符合裁決結果。

分檔方式：

- **依「最低等級」分四個檔，各自設定保留天數**：每個檔是「該等級以上」的完整串流，查錯誤只要看 error 檔，保留最久。
- **只寫一個檔、用外部工具依等級清理**：Serilog 的 File sink 無法依事件等級刪除檔案中的部分內容，做不到。

## 決策結果

採用 **選項 1**，生產環境寫四個 JSON 檔（一行一筆）：

| 檔案 | 內容 | 保留 |
|------|------|------|
| `logs/debug-YYYYMMDD.json` | 只有 Verbose、Debug（預設最低等級為 Information，平常不會產生） | 7 天 |
| `logs/info-YYYYMMDD.json` | Information 以上 | 30 天 |
| `logs/warning-YYYYMMDD.json` | Warning 以上 | 90 天 |
| `logs/error-YYYYMMDD.json` | Error 以上 | 365 天 |

- 每筆最上層欄位：`timestamp`、`level`、`message`、`service`（固定 `TcmClinic.Api`）、`environment`（ASP.NET Core 環境名稱）、`trace_id`、`span_id`；有例外時加 `exception`；其餘事件屬性收在 `properties` 物件內。
- 保留沿用每日＋單檔 100 MB 輪替，天數以 `retainedFileTimeLimit` 控制，不再限制檔數。
- 請求日誌由 `UseSerilogRequestLogging` 在請求完成時記一筆，加記 `source_ip`（`HttpContext.Connection.RemoteIpAddress`）。
- 開發環境主控台維持文字格式（`outputTemplate` 含 `{TraceId}`）。
- sink 與格式仍由設定驅動（`appsettings.Production.json`），`service`、`environment` 兩個欄位在程式中加入（`LoggerConfigs`）。

**理由**：

- JSON 可以直接匯入日誌查詢工具，也能以 `trace_id` 精確篩選同一請求，和 ProblemDetails 回應中的 `traceId` 對照（DEF-011）。
- 錯誤紀錄保留一年，事後追查客戶回報的問題時還找得到。
- 最上層欄位固定且全為 snake_case，查詢語法不受事件屬性名稱影響。

## 後果

- **好**：
  - 機器可讀，可用 `trace_id`、`level` 直接過濾。
  - 錯誤保留時間從「最多 31 個檔」延長為 1 年。
  - 請求日誌可追查來源 IP。
- **壞**：
  - 人工直接閱讀 log 檔比文字格式吃力；需要時可用 `jq` 等工具轉換，或看主控台輸出。
  - 同一筆錯誤會同時出現在 info、warning、error 三個檔，磁碟用量增加；由部署端監控磁碟容量。
  - 新增 Serilog.Expressions 套件（Apache-2.0），已登記於 `THIRD-PARTY-NOTICES.md`。
  - 部署在反向代理後方時，`source_ip` 會是代理的位址，須另外設定 Forwarded Headers。

## 連結

- 規範：[docs/LOGGING.md](../../../docs/LOGGING.md) §1～§3
- 延後事項：[specs/README.md](../../README.md) DEF-014
- 程式：`backend/src/TcmClinic.Api.Web/Configurations/LoggerConfigs.cs`、`backend/src/TcmClinic.Api.Web/appsettings.Production.json`
- 測試：`backend/tests/TcmClinic.Api.FunctionalTests/LoggingFileFormatTests.cs`
