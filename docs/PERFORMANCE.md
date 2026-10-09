# 效能規範（Performance）

> 版本 1.0.0｜最後更新 2026-10-08

> 本檔為憲章原則 VI（效能是可衡量的需求，但不凌駕核心原則）的 **How**。
> 效能永遠排在原則 I–IV 之後；不得以效能為由跳過安全檢查、放棄測試、破壞分層或硬編碼。

## 1. 效能預算（MUST）

- 每個功能規格必須為其使用者操作宣告目標。預設預算（規格可加理由調整）：
  - 讀取（Query）API 端點：p95 ≤ 300ms（nominal load）。
  - 寫入（Command）API 端點：p95 ≤ 500ms（nominal load）。
- List 端點必須分頁並強制最大頁數上限（預設 100）；**無界結果集禁止**。

## 2. 資料存取（MUST）

- **禁止 N+1**：關聯資料以 `Include`/投影或 `Ardalis.Specification` 載入，不做逐列 lazy load。
- 讀取/查詢路徑必須用 `AsNoTracking`（透過 spec），並宜投影到 DTO，而非在只需部分欄位時 materialize 完整聚合。
- 昂貴或熱門讀取路徑宜透過 pipeline behavior 快取（明確 `CacheKey`）；底層資料異動時必須考慮快取失效。

## 3. 非同步 I/O（MUST）

- 所有 I/O 必須全程非同步；**sync-over-async 禁止**（`.Result`、`.Wait()`、`.GetAwaiter().GetResult()`）。

## 4. 紀律（MUST）

- 最佳化必須以證據驅動：先量測（遙測、於 spec 用 `TagWith` 標註查詢歸因）再優化。
- 有損可讀性的過早微優化不鼓勵（YAGNI）。
- 使既有預算退步的變更，合併前必須修正或說明理由（Governance 審查閘門）。

## 5. 觀測

- 效能預算必須能在生產環境觀察（Serilog + OpenTelemetry，見 `docs/LOGGING.md`）。
- 於 spec 用 `TagWith("SpecName")` 便於在日誌中歸因慢查詢。
