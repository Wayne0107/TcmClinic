# ADR-003 所有資料表一律軟刪除，並具備五個標準欄位

| 項目 | 內容 |
|------|------|
| 狀態 | Accepted |
| 日期 | 2026-09-30（全表統一五欄的決策日；軟刪除為更早的既有做法；2026-10-06 依憲章 2.1.0 補記為 ADR） |
| 決策者 | Wayne（維護者） |
| 出處 | `docs/DATABASE-SCHEMA.md` §1、§2、§6；[SPEC-013 個人資料維護](../../specs/管理業務/系統管理作業/個人資料維護/spec.md) R8（2026-09-30 修訂） |

> 📦 本 ADR 隨範本沿用自源專案（CHB），決策本身仍適用於範本。背景、影響與相關連結中提到的舊系統、分行、企業、開狀申請、系統參數等屬源專案業務，相關規格、資料表與 migration 範本已移除；以骨架程式碼與測試為準。

## 背景（Context）

銀行業務資料需要可追溯：誰在什麼時候建立、修改、刪除了資料。早期各表各自決定稽核欄位，append-only 日誌表另用 `OperatedBy`／`OperatedAt` 等名稱，查詢與攔截器處理都不一致。刪除也需要能保留紀錄，避免單據引用的資料被實體刪除。

## 考慮過的選項

1. **所有表統一五欄 `CreateDate`／`CreateUser`／`ModifyDate`／`ModifyUser`／`Removed`，刪除一律軟刪除**，由攔截器自動填入。
2. **各表依需要自訂稽核欄位**，日誌表沿用 `OperatedBy`／`OperatedAt`。
3. **實體刪除＋另建刪除紀錄表**。
4. **UDS 預設 `created_at`／`updated_at`、UTC 時間**。

## 決策結果

採用 **選項 1**：

- 五欄宣告在共用基底類別 `AuditableEntityBase`，由 `AuditableEntityInterceptor` 自動填入；應用程式不得手動設定。
- `datetime` 存當地時間；`CreateUser`／`ModifyUser` 存登入帳號（背景作業為 `system`）。
- 刪除預設設 `Removed = 1`，全域查詢過濾器排除已刪除列；唯一性約束一律用 `Removed = 0` 的篩選索引，讓已刪除的值可以重用。
- append-only 稽核、日誌表也具備五欄（`ModifyDate`／`ModifyUser` 與建立值相同、`Removed` 恆為 0）。
- 聚合內的子實體隨聚合根一起刪除時，經攔截器同樣轉為軟刪除（需先載入子實體，見 SPEC-059 設計決策 R6）。

**理由**：格式統一後攔截器、查詢過濾器、資料表異動紀錄都能一致處理；軟刪除保留歷史且不破壞單據引用；篩選索引兼顧唯一性與重用。

## 後果

- **好**：任何資料都查得到建立者與最後修改者；刪除可追溯、可救回；新表只要繼承基底類別即自動合規。
- **壞**：資料表持續累積已刪除列；讀已刪除資料要明確 `IgnoreQueryFilters`；唯一性約束必須記得加 `Removed = 0` 篩選，否則刪除後無法重建同值資料；不採用 UTC，跨時區部署時須另行處理。

## 連結

- `docs/DATABASE-SCHEMA.md` §1、§2、§6
- [SPEC-006 資料表異動紀錄](../../specs/其他/資料表異動紀錄/spec.md)
- 程式：`backend/src/TcmClinic.Api.SharedKernel/AuditableEntityBase.cs`、`backend/src/TcmClinic.Api.Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs`
