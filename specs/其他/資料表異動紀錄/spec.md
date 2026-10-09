# [SPEC-006] Feature: 資料表異動紀錄（Data Audit Trail）

<!-- spec-type: infrastructure -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-006 |
| 規格類型 | infrastructure |
| 狀態 | Archived |
| 選單 | 無（跨選單基礎規格）；原「資料異動紀錄」`/admin/audit-logs` 已於 2026-10-02 移除 |
| 建立日期 | 2026-09-02 |
| 最後更新 | 2026-10-04（由 Spec Kit 格式轉為 UDS SDD 格式；內容未變） |
| 原分支 | `006-data-audit-trail` |
| 相關規格 | 橫切，建於既有稽核欄位機制之上（SPEC-002 [後台選單與角色權限管理](../後台選單與角色權限管理/spec.md) 設計決策 R6）；查詢授權原綁定 SPEC-005 [選單導向授權模型](../選單導向授權模型/spec.md)；← SPEC-010 [資料訪問日誌](../資料訪問日誌/spec.md) 沿用 `[SensitiveData]` 標記與稽核表例外；← SPEC-013 [個人資料維護](../../管理業務/系統管理作業/個人資料維護/spec.md)（密碼遮罩、修正 `ICurrentUser` 註冊順序使異動紀錄記錄真實登入帳號）；← SPEC-016 系統參數維護（參數修改前後值；⚠️ 修改本規格查詢清單預設每頁筆數） |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

> **狀態沿革**：Implemented（2026-09-02）；2026-09-30 修訂已完成（資料表規範對齊，見 Clarifications Session 2026-09-30；全套 179 單元 + 81 整合 + 204 功能綠）；2026-10-02 移除舊後台選單（見 Clarifications Session 2026-10-02）。
>
> ⚠️ **部分被 SPEC-016 系統參數維護修改**：異動紀錄清單預設每頁筆數改取「查詢列表分頁筆數」（其 FR-008(b)，初始值 10，由 20 改為 10）。查詢 API 已於 2026-10-02 移除；2026-10-06 SPEC-016 再移除該參數（[ADR-007](../架構決策/ADR-007-default-page-size-fixed.md)），本規格不受實際影響。
>
> ⚠️ **2026-10-02 查詢 API（US2）與「資料異動紀錄」選單移除，寫入不變**。

## Overview（概述）

資料寫入時**自動**產生「改前／改後整列快照」的異動紀錄，含操作者、發生時間與操作類型；憑證／機密欄位（密碼雜湊、salt 等）遮罩、一般個資照實記錄；異動紀錄與資料變更在**同一交易**內一致寫入，回滾不留孤兒。原始需求：「資料表異動紀錄 audit trail：於資料寫入時自動記錄『改前 / 改後』的值，含操作者與時間；敏感資料（如密碼、雜湊）必須遮罩，不得寫入異動紀錄。」原本另含最小唯讀查詢 API，已於 2026-10-02 移除。

## Motivation（動機）

- **自動記錄資料異動（US1，P1）**：沒有可靠的自動捕捉與遮罩，其餘功能都無所依附；且遮罩是安全底線，必須與捕捉一起交付，否則會把密碼等機密寫入紀錄。即使沒有任何檢視介面，異動紀錄本身即具備稽核價值。
- **查詢資料的異動歷史（US2，P2；已停止適用）**：讓已累積的異動紀錄可被人使用；但在缺少此介面時，紀錄仍可經由資料層直接查閱，故次於 P1。

## Clarifications（釐清紀錄）

### Session 2026-09-02

- Q: 本 feature 是否一併交付異動歷史查詢能力（US2），或只做捕捉、查詢延後？ → A: 捕捉 + 最小唯讀查詢 API（受權限控管），本次不做專屬前端頁面。
- Q: 誰可以讀取異動紀錄查詢 API？ → A: 綁定既有選單導向授權（005）——新增受保護的「資料異動紀錄」功能項，可見（＝可操作）該項的角色才能查詢，不另造平行授權機制。
- Q: 遮罩範圍要遮到哪裡（只遮憑證，或連一般個資也遮）？ → A: 只遮「憑證/機密」（密碼雜湊、salt、token、cookie、連線字串等）；一般個資（帳號、姓名）照實記錄，改以存取控制保護。

### Session 2026-09-30

背景：DATABASE-SCHEMA §1、§6 修訂為「所有表（含 append-only 稽核／日誌表）統一五個標準欄位」，本功能的異動紀錄表原以自訂欄位記錄操作者與時間，不符新規範。

- 異動紀錄改用系統統一的五個標準欄位記錄操作者與時間（建立者＝操作者、建立時間＝發生時間；修改者／修改時間與其同值；刪除旗標恆為否），不另設自訂欄位。既有紀錄保留並轉換，不遺失。
- 查詢 API 對外欄位與篩選參數名稱（操作者、發生時間）維持不變，前端不需改版；外部可觀察行為不變。

### Session 2026-10-02

背景：CHB 選單已併入規格（見 CHB選單與角色種子），範本早期的舊後台選單（`/admin/users`、`/admin/roles`、`/admin/audit-logs`、`/admin/access-logs`）不再使用。使用者決定直接移除並修訂相關規格，不另開規格。

- Q: 異動紀錄查詢 API 與「資料異動紀錄」選單如何處理？ → A: **移除**查詢 API（`GET /audit-logs`）與「資料異動紀錄」選單（`/admin/audit-logs`，種子軟刪除）；異動紀錄的寫入（User Story 1）不變。User Story 2、FR-011、FR-012、SC-003 停止適用，目前需要時由資料庫直接查閱；日後如需查詢畫面，由其他規格依舊系統重新定義。

## Requirements（需求）

### Requirement: US1 自動記錄資料異動（含遮罩）（P1）

The system SHALL 在受稽核資料被新增、修改或刪除時，自動產生一筆含改前值、改後值、操作類型、操作者與發生時間的異動紀錄，並遮罩機密欄位。

當任何受稽核的資料表被新增、修改或刪除時，系統必須自動產生一筆異動紀錄，忠實保存「改前值」與「改後值」、操作類型、操作者與發生時間；且對敏感欄位進行遮罩，機密內容永不進入異動紀錄。此為本功能的 MVP。獨立測試：針對一筆受稽核資料執行新增／修改／刪除，直接檢查異動紀錄儲存區，確認每次操作都留下正確的改前/改後值、操作者、時間與操作類型；並修改含機密欄位的資料，確認紀錄中該欄位為遮罩值而非明碼/雜湊。涵蓋 FR-001～FR-010、FR-013～FR-016。

#### Scenario: AC-1 修改產生「修改」紀錄
- **GIVEN** 一筆既有資料
- **WHEN** 使用者修改其中一個欄位並存檔
- **THEN** 系統產生一筆「修改」異動紀錄，內含該欄位的改前值與改後值、操作者、發生時間

#### Scenario: AC-2 新增產生「新增」紀錄
- **GIVEN** 一筆新資料
- **WHEN** 使用者新增並存檔
- **THEN** 系統產生一筆「新增」異動紀錄，改後值為新資料內容、改前值為空，並正確記錄該筆資料的識別碼

#### Scenario: AC-3 刪除產生「刪除」紀錄
- **GIVEN** 一筆既有資料
- **WHEN** 使用者刪除（含軟刪除）
- **THEN** 系統產生一筆「刪除」異動紀錄，保留刪除前的值

#### Scenario: AC-4 敏感欄位遮罩
- **GIVEN** 一筆含敏感欄位（例如密碼雜湊）的資料
- **WHEN** 該欄位被建立或變更
- **THEN** 異動紀錄中該欄位以遮罩表示，明碼與雜湊皆不得出現於紀錄

#### Scenario: AC-5 多筆異動與回滾一致
- **GIVEN** 一次存檔同時異動多筆資料
- **WHEN** 存檔成功
- **THEN** 每一筆受稽核資料各自產生對應的異動紀錄；若存檔失敗回滾，不得殘留任何對應的異動紀錄

### Requirement: US2 查詢資料的異動歷史（P2）（⚠️ 2026-10-02 移除，停止適用；見 Clarifications Session 2026-10-02）

The system SHALL（停止適用）讓具權限的管理／稽核人員透過最小唯讀查詢 API 查詢特定資料的異動歷史，或特定操作者於期間內的所有異動。

具權限的管理／稽核人員能透過一個**最小的唯讀查詢 API**查詢特定一筆資料的完整異動歷史（依時間排序的改前/改後、操作者），或查詢特定操作者在期間內的所有異動，以支援問題追查與稽核。本次**不含專屬前端檢視頁面**（可留待日後 feature）。獨立測試：對同一筆資料連續做多次修改後，透過查詢介面取得該筆資料依時間排序的異動清單，確認每次變更的改前/改後、操作者與時間正確且完整。涵蓋 FR-011、FR-012。

#### Scenario: AC-6 查詢特定資料的異動歷史
- **GIVEN** 某筆資料經過多次修改
- **WHEN** 稽核人員查詢該筆資料的異動歷史
- **THEN** 系統回傳依時間排序的異動清單，每筆含操作類型、操作者、時間與變更內容

#### Scenario: AC-7 依操作者與期間查詢
- **GIVEN** 一位操作者在某期間內做過多次異動
- **WHEN** 稽核人員以操作者與期間查詢
- **THEN** 系統回傳該操作者於該期間的所有異動紀錄

#### Scenario: AC-8 未具權限者拒絕
- **GIVEN** 一位未具稽核權限的使用者
- **WHEN** 其嘗試查詢異動歷史
- **THEN** 系統拒絕存取

### 功能需求條文（Functional Requirements）

- **FR-001**: 系統 MUST 在受稽核資料被新增、修改或刪除並成功持久化時，自動產生對應的異動紀錄，無需用例／應用層手動呼叫。
- **FR-002**: 每筆異動紀錄 MUST 至少包含：受異動的資料表／實體識別、該筆資料的主鍵、操作類型（新增／修改／刪除）、操作者、發生時間、改前值與改後值。
- **FR-003**: 對「修改」操作，異動紀錄 MUST 能呈現實際發生變更的欄位之改前值與改後值。
- **FR-004**: 系統 MUST NOT 將**憑證/機密類**資料（密碼、密碼雜湊、salt、token、cookie、完整連線字串等）以明文寫入異動紀錄；此類欄位 MUST 以遮罩值取代或排除。一般識別性個資（如帳號、姓名）照實記錄，改以查詢 API 的存取控制（FR-012）保護，不予遮罩，以維持稽核可追查性。
- **FR-005**: 敏感欄位 MUST 以宣告式方式標記（而非分散在流程各處的臨機判斷），使新增敏感欄位時能一致地被遮罩。
- **FR-006**: 異動紀錄的產生 MUST 與觸發它的資料變更具一致性——資料變更被回滾時，對應異動紀錄不得殘留；異動紀錄寫入失敗時，資料變更亦不得被視為成功。
- **FR-007**: 系統 MUST 正確記錄由資料庫產生之識別碼的新增資料，使異動紀錄關聯到正確的最終主鍵。
- **FR-008**: 對未實際改變任何值的存檔，系統 MUST NOT 產生空洞的異動紀錄。
- **FR-009**: 操作者來源 MUST 沿用系統既有的「當前操作者」機制；在系統背景作業情境下 MUST 記錄可識別的系統操作者標識。
- **FR-010**: 異動紀錄一經產生 MUST 為僅供新增（append-only），一般操作路徑不得竄改或刪除既有紀錄。
- **FR-011**: 具權限者 MUST 能依「特定資料」查詢其依時間排序的異動歷史，並能依「操作者＋期間」查詢異動紀錄。（⚠️ 2026-10-02 移除，停止適用；見 Clarifications Session 2026-10-02）
- **FR-012**: 查詢異動紀錄的能力 MUST 受既有選單導向授權模型（005）控管——透過一個受保護的「資料異動紀錄」功能項，僅可見（＝可操作）該功能項的角色能存取；未具該權限者不得存取，且不另建平行授權機制。（⚠️ 2026-10-02 移除，停止適用；見 Clarifications Session 2026-10-02）
- **FR-013**: 系統 MUST 有一套護欄測試，確保敏感欄位不會因日後程式變更而洩漏進異動紀錄。
- **FR-014**: 受稽核範圍 MUST 為「預設全覆蓋所有具稽核欄位的資料表，並提供一份可設定的排除清單」；被列入排除清單的資料表不產生異動紀錄。排除清單 MUST 以設定／宣告方式維護，變更範圍時無需改寫核心記錄流程。
- **FR-015**: 異動紀錄的呈現粒度 MUST 為「每次異動一筆整列快照」，於同一筆紀錄中保存該筆資料的改前值與改後值（各為完整列的內容）。
- **FR-016**: 異動紀錄 MUST 永久保留；系統本身不自動清理，清理由維運另行執行。此策略 MUST 記錄為已知的磁碟成長取捨。

### 邊界情境（Edge Cases）

- 當某次存檔實際上沒有改變任何欄位值（no-op 更新）時，是否仍產生異動紀錄？（預設：不產生，只有實際值變動才記錄。）
- 當異動紀錄本身寫入失敗時，資料變更是否應一併失敗？（預設：異動紀錄與資料變更視為同一交易，任一失敗則整體回滾，確保稽核完整。）
- 當一筆新資料的識別碼由資料庫產生時，異動紀錄必須仍能正確關聯到該筆資料的最終識別碼。
- 當受稽核欄位為關聯集合或巢狀值物件時，改前/改後值如何呈現而不遺漏變更。
- 當敏感欄位清單日後新增欄位時，既有機制必須能涵蓋而不需逐一改動記錄流程。

### 關鍵實體（Key Entities）

- **異動紀錄（Audit Record）**：代表一次對某筆受稽核資料的寫入事件。關鍵屬性：目標實體／資料表名稱、目標資料主鍵、操作類型（新增／修改／刪除）、改前值、改後值、操作者、發生時間。與系統中各受稽核實體為「多對一」的邏輯關聯（透過實體名稱＋主鍵），本身不修改被稽核的資料。
- **敏感欄位標記（Sensitive Field Marker）**：對實體欄位的宣告式標註，用以指示該欄位在產生異動紀錄時必須被遮罩或排除。

### 假設（Assumptions）

- 沿用專案既有的稽核欄位機制（建立者/修改者/時間）與「當前操作者」抽象作為操作者來源。
- 異動紀錄與被稽核資料存放於同一資料庫，使兩者可在同一交易中一致寫入。
- 本功能僅記錄「寫入」操作（新增／修改／刪除），不記錄純讀取。
- 異動紀錄的遮罩對齊憲章原則 XVII 對「憑證/機密」的 MUST NOT 記錄要求；但個資（帳號/姓名）在此**受權限控管的稽核表**中照實記錄（以存取控制取代遮罩），此為與「一般日誌須遮罩 PII」的刻意區隔，理由為維持稽核可追查性。稽核欄位機制對齊原則 XI。
- 保留策略為「永久保留、由維運另行清理」，呼應先前不採用自動刪除的偏好（已知取捨：資料庫會持續成長）。
- 受稽核範圍預設全覆蓋所有具稽核欄位的資料表，並以排除清單關閉不需記錄者。
- 每次異動以「整列快照」保存改前/改後值；逐欄位差異可由改前/改後快照比對得出。
- 查詢能力本次以「最小唯讀查詢 API（受權限控管）」交付，不含專屬前端檢視頁面；日後如需 UI 另開 feature。（⚠️ 2026-10-02 查詢 API 已移除）

## Acceptance Criteria（驗收條件）

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、SC-001、FR-003、FR-015 | 修改產生一筆 Update，改前／改後整列快照反映變更 | `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AuditLogInterceptorTests.cs` › `Update_creates_audit_record_with_old_and_new`；`backend/tests/TcmClinic.Api.UnitTests/Auditing/AuditSnapshotBuilderTests.cs` › `Classifies_modified_as_update` |
| AC-2 | US1 情境 2、SC-001、FR-007 | 新增產生一筆 Insert，`OldValues` 為 null、主鍵正確 | `AuditLogInterceptorTests` › `Insert_creates_audit_record_with_pk`；`AuditSnapshotBuilderTests` › `Classifies_added_as_insert` |
| AC-3 | US1 情境 3、SC-001 | 刪除（含軟刪除）產生一筆 Delete，保留刪除前值、`NewValues` 為 null | `AuditLogInterceptorTests` › `Delete_creates_delete_record`；`AuditSnapshotBuilderTests` › `Classifies_soft_delete_as_delete`、`Classifies_deleted_as_delete` |
| AC-4 | US1 情境 4、SC-002、FR-004、FR-013 | 密碼雜湊與 salt 於快照中為 `"***"`，帳號照實（敏感資料出現率 0%） | `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AuditRedactionTests.cs` › `Password_hash_and_salt_are_masked_in_audit`；`AuditSnapshotBuilderTests` › `Serializes_mask_value_as_is` |
| AC-5 | US1 情境 5、SC-004、FR-006 | 回滾後孤兒異動紀錄數＝0；一次多筆異動各自產生紀錄 | `AuditLogInterceptorTests` › `Rollback_leaves_no_orphan_audit`；多筆部分：手動：Code Review 確認 `AuditLogInterceptor` 對物化後的每個受稽核 entry 各產生一筆（設計決策 R1、R7） |
| AC-6 | US2 情境 1、SC-003、FR-011 | （停止適用）依資料查詢依時間排序的歷史 | 停止適用（2026-10-02 查詢 API 移除，原 `GetAuditLogsQueryTests` 一併刪除）；移除本身由 AC-13 驗證 |
| AC-7 | US2 情境 2、FR-011 | （停止適用）依操作者＋期間查詢 | 停止適用（同 AC-6）；移除由 AC-13 驗證 |
| AC-8 | US2 情境 3、FR-012 | （停止適用）未具權限者拒絕存取 | 停止適用（原 `GetAuditLogsAuthorizationTests` 一併刪除）；移除由 AC-13 驗證 |
| AC-9 | SC-005、FR-005、FR-014 | 預設全覆蓋；`[NotAudited]` 實體與 `AuditLog` 本身被排除；新增標記即涵蓋，不改核心流程 | `backend/tests/TcmClinic.Api.UnitTests/Auditing/AuditScopeTests.cs` › `Ordinary_entity_is_audited`、`NotAudited_entity_is_excluded`、`AuditLog_itself_is_excluded` |
| AC-10 | FR-008 | 無實質變更（僅稽核欄變動）不產生紀錄 | `AuditLogInterceptorTests` › `Noop_save_creates_no_record`；`AuditSnapshotBuilderTests` › `Only_metadata_change_is_noop`、`Business_change_is_detected` |
| AC-11 | FR-002、FR-009、Clarifications 2026-09-30 | 五個標準欄位：建立者＝操作者（背景作業為 `system`）、修改者與時間同建立、未刪除；已登入請求記錄真實帳號；綱要無 `OperatedBy`／`OperatedAt`、索引採 EF 預設命名 | `AuditLogInterceptorTests` › `Audit_record_has_standard_columns_filled`；`backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AuditLogSchemaTests.cs` › `SysAuditLog_schema_matches_design`；`backend/tests/TcmClinic.Api.FunctionalTests/CurrentUserResolutionTests.cs` › `Authenticated_request_records_real_account` |
| AC-12 | FR-010、FR-016 | 紀錄 append-only、永久保留，無一般路徑更新／刪除，無自動清理 | 手動：Code Review 確認 `AuditLogConfig` 未設 TTL／清理、程式中無更新或刪除 `AuditLog` 的路徑 |
| AC-13 | Clarifications 2026-10-02 | 「資料異動紀錄」選單 `/admin/audit-logs` 不再種入、既有開發庫經種子軟刪除，admin 可見選單不含之 | `backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbMenuTreeSeedTests.cs` › `Admin_menus_keep_only_menu_management`、`Reseeding_removes_retired_admin_menus`；`backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs` › `Seeded_admin_sees_menus_and_can_access` |

> AC-6～AC-8：US2 已於 2026-10-02 停止適用，列出僅供追溯；其原驗證測試已隨查詢 API 移除（commit `c8e5872`）。
>
> AC-11：已登入請求記錄真實帳號，係 SPEC-013 個人資料維護 修正 `ICurrentUser` 註冊順序（其設計決策 R14）後才成立。

## Technical Design（技術設計）

### 技術脈絡

- 語言／版本：C# / .NET 10（TFM `net10.0`）；本功能為後端橫切，無前端工作。
- 主要相依（皆既有，沿用）：EF Core（`SaveChangesInterceptor`）、FastEndpoints、Mediator、Ardalis.Result / Ardalis.Specification、`Ardalis.SmartEnum`（操作類型）、`System.Text.Json`（快照序列化，.NET 內建）。**不新增第三方套件**。
- 儲存：EF Core → SQL Server。**新增一張資料表 `SysAuditLog`**（append-only）。被稽核資料與異動紀錄同庫，兩者於同一 `SaveChanges`／交易一致寫入。
- 測試：xUnit v3、NSubstitute、Testcontainers（整合）、`WebApplicationFactory`（功能）；每個 `[Fact]`/`[Theory]` 帶 zh-TW `DisplayName`（Principle X）。含機密遮罩護欄測試（Principle XV/XVII、FR-013）。
- 目標平台：Linux/Windows 伺服器（API）；monorepo（`backend/` + `frontend/` + `apphost/`），本功能僅動 `backend/`。
- 效能目標（沿用預設，Principle XIV）：寫入 ≤ 500ms p95、查詢 ≤ 300ms p95。稽核於寫入路徑增加「掃描異動 entry + 序列化 JSON」成本，與異動實體數成正比；主鍵採 DB 產生（見設計決策 R3）避免 O(n) 掃表。
- 限制：
  - **同交易一致性**（FR-006）：`AuditLog` 列在**同一** `SaveChanges` 內加入，隨資料變更一起 commit/rollback；回滾不留孤兒紀錄。
  - **主鍵時機**（FR-007）：被稽核實體採客戶端 `ValueGenerator`（`GeneratesTemporaryValues=false`、`ValueGeneratedOnAdd`），主鍵於 `Add` 當下即指派，`SavingChanges` 期間已可讀取。
  - **攔截器順序**：`AuditLogInterceptor` 必須登記於 `AuditableEntityInterceptor` **之後**，才能：①在「新值」快照中含攔截器剛填入的 `Modify*`；②把硬刪除已被轉成的 `Modified + Removed=1` 判為「刪除」。
  - **no-op 判定**（FR-008）：判斷「是否真的有變更」時，**必須排除**稽核欄（`CreateDate/User`、`ModifyDate/User`），否則攔截器每次都會刷新 `ModifyDate`，導致無實質變更也被記錄。
- 規模：後台管理者規模；`SysAuditLog` **永久保留**（FR-016），將持續成長（已知取捨，見原則符合性）。
- 技術取向摘要：新增 `AuditLogInterceptor : SaveChangesInterceptor`（登記於既有 `AuditableEntityInterceptor` 之後），於 `SavingChangesAsync` 掃描 `ChangeTracker`，比對 `OriginalValues`/`CurrentValues` 產生整列快照，以宣告式 `[SensitiveData]` 標記遮罩機密欄位，並把 `AuditLog` 列 `Add` 進同一個 `DbContext`（同交易）。查詢面原提供最小唯讀 API `GET /audit-logs`（分頁 + 篩選），授權綁定 SPEC-005 新增的「資料異動紀錄」功能項（route `/admin/audit-logs`）；2026-10-02 已移除。

### 受影響元件

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.SharedKernel/SensitiveDataAttribute.cs` | 新增：標記需遮罩之屬性（憑證/機密） |
| `backend/src/TcmClinic.Api.SharedKernel/NotAuditedAttribute.cs` | 新增：標記整個實體排除稽核（排除清單，FR-014） |
| `backend/src/TcmClinic.Api.Core/Auditing/AuditLog.cs` | 新增：append-only 稽核實體（`IAggregateRoot`、`long Id`；2026-09-30 起實作 `IAuditable`、`ISoftDelete`） |
| `backend/src/TcmClinic.Api.Core/Auditing/AuditOperation.cs` | 新增：SmartEnum `Insert`/`Update`/`Delete` |
| `backend/src/TcmClinic.Api.Core/UserAggregate/User.cs` | 修改：`PasswordHash`、`PasswordSalt` 加 `[SensitiveData]` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/AppDbContext.cs` | 修改：新增 `DbSet<AuditLog>` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Config/AuditLogConfig.cs` | 新增：`SysAuditLog` 綱要、IDENTITY 主鍵、全欄 `HasComment`、索引 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Interceptors/AuditLogInterceptor.cs` | 新增：捕捉改前/改後、遮罩、同交易寫入 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Auditing/AuditSnapshotBuilder.cs` | 新增：由 entry 產生（遮罩後）old/new JSON、操作分類、no-op 判定 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Auditing/AuditScope.cs` | 新增：判定實體是否受稽核（排除 `[NotAudited]` 與 `AuditLog` 本身） |
| `backend/src/TcmClinic.Api.Infrastructure/InfrastructureServiceExtensions.cs` | 修改：註冊 `AuditLogInterceptor`（於 `AuditableEntityInterceptor` 之後） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20260902121532_AddAuditLog.cs` | 新增：建立 `SysAuditLog` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20260930094827_SysAuditLogStandardColumns.cs` | 2026-09-30：改為五個標準欄位、索引改 EF 預設命名 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/SeedData.cs` | 原種入「資料異動紀錄」選單並授予 admin 可見性；2026-10-02 起改為軟刪除該選單 |

已於 2026-10-02（commit `c8e5872`）刪除的查詢面元件：`backend/src/TcmClinic.Api.Core/Auditing/Specifications/AuditLogSpecs.cs`、`backend/src/TcmClinic.Api.UseCases/Auditing/GetAuditLogs.cs`、`AuditLogDto.cs`、`backend/src/TcmClinic.Api.Web/Endpoints/Auditing/GetAuditLogsEndpoint.cs` 及其功能測試。

Structure Decision：沿用既有 monorepo 與分層。核心新增為 Infrastructure 的捕捉攔截器 + 快照/遮罩、Core 的 `AuditLog` 實體 + `AuditOperation` SmartEnum。`AuditLog` 置於 Core 使 UseCases 能以 `IReadRepository<AuditLog>` 查詢（UseCases 僅依賴 Core）。

### 資料模型

#### 實體：AuditLog（資料表 `SysAuditLog`）

append-only、不可變的異動紀錄（FR-010）。置於 `TcmClinic.Api.Core/Auditing/`，實作 `IAggregateRoot`（供 `IReadRepository<AuditLog>` 查詢）與 `IAuditable`、`ISoftDelete`。**不**繼承 `AuditableEntityBase`（僅因主鍵採原生 `long`，見原則符合性的例外紀錄）；DATABASE-SCHEMA §1 五個標準欄位照常具備（2026-09-30 修訂，設計決策 R12）。

| 屬性 | 型別 | DB 欄位 | 說明 / 約束 | 對應 FR |
|------|------|---------|-------------|---------|
| `Id` | `long` | `bigint IDENTITY`，PK | DB 產生、單調遞增（偏離 Principle VII，justified） | — |
| `EntityName` | `string` | `nvarchar(128)`, NOT NULL | 受異動之 CLR 實體型別名（如 `User`） | FR-002 |
| `TableName` | `string` | `nvarchar(128)`, NOT NULL | 對應資料表名（如 `SysUser`） | FR-002 |
| `PrimaryKey` | `string` | `nvarchar(256)`, NOT NULL | 目標資料列主鍵字串（複合鍵以 `k=v;k=v` 表示；強型別取底層值） | FR-002 / FR-007 |
| `Operation` | `AuditOperation`（SmartEnum） | `nvarchar(10)`, NOT NULL | `Insert` / `Update` / `Delete`（存 `Name`） | FR-002 |
| `OldValues` | `string?` | `nvarchar(max)`, NULL | 改前整列快照 JSON（Insert 為 `null`；機密已遮罩） | FR-002 / FR-003 / FR-004 / FR-015 |
| `NewValues` | `string?` | `nvarchar(max)`, NULL | 改後整列快照 JSON（Delete 為 `null`；機密已遮罩） | FR-002 / FR-003 / FR-004 / FR-015 |
| `CreateDate` | `DateTime` | `datetime`, NOT NULL | 發生時間（本地時區；INSERT 時設定一次） | FR-002 |
| `CreateUser` | `string` | `nvarchar(50)`, NOT NULL | 操作者（`ICurrentUser.UserName`；背景作業為系統標識） | FR-002 / FR-009 |
| `ModifyDate` | `DateTime` | `datetime`, NOT NULL | 與 `CreateDate` 同值（列不會被更新） | — |
| `ModifyUser` | `string` | `nvarchar(50)`, NOT NULL | 與 `CreateUser` 同值（列不會被更新） | — |
| `Removed` | `bool` | `bit`, NOT NULL, 預設 `0` | 恆為 `0`（列不會被軟刪除） | FR-010 |

**索引**：
- PK 叢集索引於 `Id`。
- 非叢集索引 `(EntityName, PrimaryKey, CreateDate)`：支撐「查特定資料的異動歷史」（FR-011）。
- 非叢集索引 `(CreateUser, CreateDate)`：支撐「依操作者 + 期間查詢」（FR-011）。
- 索引採 EF 預設命名（`IX_SysAuditLog_EntityName_PrimaryKey_CreateDate`、`IX_SysAuditLog_CreateUser_CreateDate`；DATABASE-SCHEMA §8）；2026-09-30 前為 `IX_SysAuditLog_Entity_Pk_Time`、`IX_SysAuditLog_Operator_Time`，欄位為 `OperatedBy`／`OperatedAt`。

**欄位描述（Principle XI）**：每一欄與資料表本身皆以 `.HasComment("…")`（zh-TW）產生 `MS_Description`。五個標準欄位以 `HasAuditColumnComments()` 套用共用描述，`CreateDate`／`CreateUser` 再覆寫為本表語意（發生時間／操作者）；其餘欄位描述於 `AuditLogConfig` 就地撰寫。

**不變性**：列不會被更新（`Modify*` 恆等於 `Create*`）、不會被軟刪除（`Removed` 恆為 `0`）。一般寫入路徑不得更新/刪除既有列（FR-010）；與一般表相同套用全域軟刪除查詢過濾器（DATABASE-SCHEMA §6）。

**五欄填入**：`AuditLogInterceptor` 登記於 `AuditableEntityInterceptor` 之後（設計決策 R4），其新增的 `AuditLog` 列不會再經過後者，故由 `AuditLog` 建構子以攔截器傳入的操作者與時間一次填入四個審計欄位（設計決策 R12）。

**API 對應**：查詢 API 對外仍用 `operatedBy`／`operatedAt`（契約不變），分別由 `CreateUser`／`CreateDate` 投影。（⚠️ 查詢 API 已於 2026-10-02 移除）

#### 值物件 / 列舉：AuditOperation（SmartEnum）

`Ardalis.SmartEnum`（Principle II，不用 plain enum）。成員：`Insert`、`Update`、`Delete`。以 `Name`（字串）持久化，便於查詢可讀與日後擴充。

中英對照（規格以中文描述、實作以英文成員名）：`Insert`＝新增、`Update`＝修改、`Delete`＝刪除（含軟刪除轉態）。

#### 標記屬性（SharedKernel）

| 型別 | 適用對象 | 作用 |
|------|---------|------|
| `SensitiveDataAttribute` | 屬性（property） | 標記該欄為憑證/機密，快照時以 `"***"` 遮罩（FR-004/005） |
| `NotAuditedAttribute` | 類別（class） | 標記整個實體排除稽核（排除清單，FR-014） |

**初始標註**：`User.PasswordHash`、`User.PasswordSalt` 加 `[SensitiveData]`（clarify Q3：僅憑證遮罩，帳號/姓名不標）。`AuditLog` 恆排除（由 `AuditScope` 硬性排除，亦可加 `[NotAudited]` 作雙重保險）。

#### 捕捉相關（Infrastructure，非持久化型別）

- `AuditScope`：判定某 entry 是否受稽核（受映射、非 `[NotAudited]`、非 `AuditLog`）。
- `AuditSnapshotBuilder`：由 `EntityEntry` 產出 `(Operation, OldValues, NewValues)`；套用遮罩；執行 no-op 判定（排除稽核欄，見設計決策 R5）；強型別/值物件取底層原生值再序列化。

#### 關聯與完整性

- `AuditLog` 與被稽核實體為**邏輯關聯**（`EntityName` + `PrimaryKey`），**不建外鍵**：目標可為任何表、且需在目標被硬刪除後仍保留紀錄，故不以 FK 綁定（此為 append-only 稽核表的正當設計，非違反 Principle XI 外鍵規則——該規則規範「實體間業務關聯」，此處為跨表通用稽核指標）。
- 與資料變更**同交易**寫入（FR-006）：由 `AuditLogInterceptor` 於同一 `SaveChanges` 加入。

#### 對照規格實體

| 規格 Key Entity | 實作對應 |
|-----------------|---------|
| 異動紀錄（Audit Record） | `AuditLog` / `SysAuditLog` |
| 敏感欄位標記（Sensitive Field Marker） | `SensitiveDataAttribute` |

### API 契約

#### 原 contracts/audit-capture.md — 契約：異動捕捉行為（AuditLogInterceptor）

規範寫入時如何產生異動紀錄。此為**行為契約**（非 HTTP），由整合/單元測試驗證。

##### 觸發

於 `AppDbContext.SaveChanges(Async)` 時，`AuditLogInterceptor`（登記於 `AuditableEntityInterceptor` **之後**）於 `SavingChanges(Async)` 掃描 `ChangeTracker.Entries()`。

##### 受稽核判定（AuditScope）

一個 entry 受稽核 ⇔ 全部成立：
1. 其 CLR 型別為 `AppDbContext` 映射之實體；
2. 型別**未**標記 `[NotAudited]`；
3. 型別**不是** `AuditLog`（防遞迴）；
4. `entry.State ∈ {Added, Modified, Deleted}`。

##### 操作分類（與攔截器順序無關）

| entry 狀態 | 條件 | Operation |
|-----------|------|-----------|
| `Added` | — | `Insert` |
| `Deleted` | — | `Delete` |
| `Modified` | `Removed` 由 `false`→`true` | `Delete`（軟刪除） |
| `Modified` | 其餘 | `Update` |

##### no-op 略過（FR-008）

對 `Update`：計算 `entry.Properties.Where(p => p.IsModified)` 並**排除**稽核欄（`CreateDate`、`CreateUser`、`ModifyDate`、`ModifyUser`）。若排除後**無**任何屬性變更 → **不產生**紀錄。（`Insert`/`Delete`/軟刪除轉態一律產生。）

##### 快照與遮罩（FR-002/003/004/015）

- `OldValues`：由 `entry.OriginalValues` 產生整列鍵值 JSON；`Insert` 時為 `null`。
- `NewValues`：由 `entry.CurrentValues` 產生整列鍵值 JSON；`Delete`（含軟刪除）時為 `null`。
- 值取用：強型別 ID / Vogen 值物件取其底層原生值再序列化。
- **遮罩**：屬性標記 `[SensitiveData]` 者，其值以字串 `"***"` 取代後才序列化。一般個資（帳號、姓名）**不遮罩**。

##### 紀錄欄位填入

| 欄位 | 來源 |
|------|------|
| `EntityName` | entry CLR 型別名 |
| `TableName` | EF 模型之資料表名 |
| `PrimaryKey` | entry 主鍵值（已於 `Add` 時指派，見設計決策 R2；複合鍵 `k=v;k=v`） |
| `Operation` | 上表分類 |
| `OldValues` / `NewValues` | 上述快照 |
| `CreateUser` | `ICurrentUser.UserName`（背景作業為系統標識，FR-009） |
| `CreateDate` | `DateTime.Now`（本地時區） |
| `ModifyUser` / `ModifyDate` | 與 `CreateUser` / `CreateDate` 同值（列不會被更新） |
| `Removed` | `false`（列不會被軟刪除） |

四個審計欄位由 `AuditLogInterceptor` 傳入 `AuditLog` 建構子填入：本攔截器登記在 `AuditableEntityInterceptor` 之後，新增的 `AuditLog` 列不會再經過後者（設計決策 R12，2026-09-30 修訂）。

##### 一致性（FR-006）

- 產生之 `AuditLog` 列以 `context.Add(...)` 加入**同一** `DbContext`，隨原變更於同一次 `SaveChanges`/交易提交。
- 資料變更回滾 → 對應 `AuditLog` 不留存（同交易，SC-004 孤兒數 = 0）。
- `AuditLog` 寫入失敗 → 整體 `SaveChanges` 失敗，資料變更不被視為成功。

##### 驗收對照

| 情境 | 期望 | 來源 |
|------|------|------|
| 修改一欄並存檔 | 1 筆 `Update`，含該欄改前/改後、操作者、時間 | US1-1 / FR-003 |
| 新增並存檔 | 1 筆 `Insert`，`OldValues=null`、正確主鍵 | US1-2 / FR-007 |
| 刪除（含軟刪除） | 1 筆 `Delete`，保留刪除前值 | US1-3 |
| 含機密欄位變更 | 該欄快照為 `"***"`；明碼/雜湊不出現 | US1-4 / FR-004 / SC-002 |
| 一次多筆異動 | 每筆各一紀錄；失敗回滾則全無 | US1-5 / FR-006 |
| 無實質變更之存檔 | 不產生紀錄 | Edge / FR-008 |
| `[NotAudited]` 實體 / `AuditLog` 本身 | 不產生紀錄 | FR-014 |

#### 原 contracts/audit-query-api.md — 契約：異動紀錄查詢 API

> ⚠️ **2026-10-02 已移除**：本查詢 API 與其選單已刪除（見 Clarifications Session 2026-10-02），以下內容僅保留作歷史紀錄；紀錄的寫入不受影響。

最小唯讀查詢 API（clarify Q1=B）。授權綁定既有選單導向模型（005，clarify Q2=A）。

##### `GET /audit-logs`

分頁列出異動紀錄，可依資料或操作者+期間篩選；依發生時間（`CreateDate`）**由新到舊**排序。

> 2026-09-30 修訂（設計決策 R12）：資料表欄位改為五個標準欄位，本 API 對外名稱不變——`operatedBy` 對應 `CreateUser`、`operatedAt` 對應 `CreateDate`。

###### 授權（FR-012）

- `Policies("MenuAccess:/admin/audit-logs")`——僅對「資料異動紀錄」選單項（route `/admin/audit-logs`）具可見性（＝可操作）之角色可存取。
- 未登入 → 401；已登入但無該選單可見性 → 403（`ProblemDetails`）。

###### 查詢參數（皆選填；可組合）

| 參數 | 型別 | 說明 |
|------|------|------|
| `entityName` | string | 篩選受異動實體型別名（如 `User`）；配合 `primaryKey` 查特定資料歷史 |
| `primaryKey` | string | 篩選目標資料主鍵字串 |
| `operatedBy` | string | 篩選操作者（比對 `CreateUser`） |
| `from` | date-time | 起始時間（含），比對 `CreateDate` |
| `to` | date-time | 結束時間（含），比對 `CreateDate` |
| `page` | int | 頁碼，預設 1 |
| `pageSize` | int | 每頁筆數，未指定、0 或負數時取系統參數「查詢列表分頁筆數」（初始 10；系統參數維護 FR-008(b)），最大 100（2026-10-02 前預設 20） |

###### 回應 200（`application/json`）

```jsonc
{
  "items": [
    {
      "id": 1024,
      "entityName": "User",
      "tableName": "SysUser",
      "primaryKey": "12",
      "operation": "Update",
      "oldValues": "{\"Account\":\"alice\",\"DisplayName\":\"Alice\",\"PasswordHash\":\"***\"}",
      "newValues": "{\"Account\":\"alice\",\"DisplayName\":\"Alice Wang\",\"PasswordHash\":\"***\"}",
      "operatedBy": "admin",
      "operatedAt": "2026-09-02T14:03:22"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137
}
```

- 分頁另以 GitHub 風格 `Link` 標頭提供 `first`/`prev`/`next`/`last`（沿用 `GetUsersEndpoint` 慣例，Principle XIII）。
- `oldValues`/`newValues` 為快照 JSON 字串；憑證/機密欄位已為 `"***"`（FR-004）。

###### 狀態碼

| 碼 | 意義 |
|----|------|
| 200 | 查詢成功（可能為空清單） |
| 401 | 未登入 |
| 403 | 無「資料異動紀錄」選單權限 |

###### 典型用法

- **查特定資料歷史**（FR-011）：`GET /audit-logs?entityName=User&primaryKey=12`
- **依操作者 + 期間**（FR-011）：`GET /audit-logs?operatedBy=admin&from=2026-09-01T00:00:00&to=2026-09-02T23:59:59`

##### 對照規格

| 契約項目 | 來源 |
|----------|------|
| 依資料查歷史、依操作者+期間查詢 | US2-1 / US2-2 / FR-011 |
| 未具權限拒絕 | US2-3 / FR-012 |
| 快照含遮罩 | FR-004 / SC-002 |

### 設計決策（原 research）

> 決策編號沿用原 research，供程式碼註解追溯。格式：決定／理由／替代方案。文中「clarify Q*」指規格釐清時的題號，原樣保留。

#### R1 捕捉機制：專用 SaveChangesInterceptor
- **決定**：新增 `AuditLogInterceptor : SaveChangesInterceptor`，於 `SavingChanges`/`SavingChangesAsync` 掃描 `context.ChangeTracker.Entries()`，對受稽核且狀態為 Added/Modified/Deleted 的 entry 產生 `AuditLog` 列，並以 `context.Add(auditLog)` 加入**同一** `DbContext`，隨原變更於同一次 SaveChanges 持久化。
- **理由**：沿用專案既有攔截器慣例（`AuditableEntityInterceptor`、Principle VI 事件派送攔截器）；`ChangeTracker` 同時握有 `OriginalValues`（改前）與 `CurrentValues`（改後），是取得「整列快照」最直接且與用例層解耦之處（FR-001「無需用例層手動呼叫」）。加入同一 SaveChanges 即天然同交易（FR-006）。
- **替代方案**：
  - SQL Server Temporal Tables：零程式碼但無法記「操作者」、無法遮罩憑證、schema 變更受限，且非本 clarify 決定的方案 A。
  - 領域事件：欄位級改前/改後不易表達，且非所有寫入都發事件。
  - 用例層手動記錄：違背 FR-001（易遺漏、侵入每個 handler）。

#### R2 主鍵時機與同交易寫入（FR-006 / FR-007）
- **決定**：被稽核實體的主鍵於 `SavingChanges` 期間**已可讀取**，故直接自 entry 取主鍵、與 `AuditLog` 列於同一 SaveChanges 寫入即滿足同交易與正確主鍵；無需二階段（SavedChanges 補鍵）。
- **理由**：本專案所有 `ValueGenerator`（`UserIdValueGenerator` 等）皆 `GeneratesTemporaryValues => false` 且 `ValueGeneratedOnAdd()`——EF 於 `Add`/`AddRange`（`SavingChanges` 之前）即指派最終客戶端 Id（非暫時值）。因此 Added 實體在攔截器內已持有正確主鍵。
- **替代方案**：二階段（`SavingChanges` 收集 + `SavedChangesAsync` 讀 DB 產生鍵再寫入）——僅在主鍵由 DB 產生（IDENTITY/temporary）時必要；本專案不需，且二階段還需顯式交易包住兩次寫入才能保原子性，較複雜。

#### R3 AuditLog 主鍵策略（偏離 Principle VII，justified）
- **決定**：`SysAuditLog` 主鍵採 DB `bigint IDENTITY`，`AuditLog` 實體以原生 `long Id`，**不**使用 Vogen ID / 客戶端 ValueGenerator，亦**不**繼承 `AuditableEntityBase`。
- **理由**：`MaxIdCalculator` 以 `context.Set<T>().AsEnumerable().Max()` 於**每次寫入**把整表載入記憶體算 Max+1（O(n)）。稽核表**永久保留**（FR-016）且每次資料變更都插入，成長後 O(n) 掃表在寫入路徑不可接受（違 Principle XIV）。IDENTITY 為 O(1)、並發安全。稽核表為 append-only、不被其他聚合以強型別 FK 參考，故 Vogen「跨聚合防誤用」的理由在此不成立。憲章亦明載 Vogen+IDENTITY 會觸發 `GetHashCode` 執行期例外，故不混用。
- **替代方案**：Vogen+Max+1（記憶體/延遲無界成長）、Vogen+IDENTITY（執行期例外風險）——皆否決。已記於原則符合性的例外紀錄。
- **2026-09-30 修訂**：「不繼承 `AuditableEntityBase`」的理由僅剩主鍵型別；五個標準欄位改為照常具備，見 R12。

#### R4 操作類型分類與攔截器順序
- **決定**：`AuditLogInterceptor` 登記於 `AuditableEntityInterceptor` **之後**。操作分類（順序無關、以下皆判定正確）：
  - `Added` → **Insert**
  - `Deleted` → **Delete**（硬刪除；若 `AuditableEntityInterceptor` 先執行已轉為 `Modified`，見下）
  - `Modified` 且 `ISoftDelete.Removed` 由 `false`→`true` → **Delete**（軟刪除）
  - 其餘 `Modified` → **Update**
- **理由**：`AuditableEntityInterceptor` 會把 `Deleted` 轉成 `Modified + Removed=1` 並刷新 `Modify*`。登記在其後，「新值」快照才含剛填入的 `Modify*`（FR-002 完整性）；以「`Removed` 轉態」判定刪除，使分類與攔截器順序無關。
- **替代方案**：登記在前（看得到原始 `Deleted`，但新值缺 `Modify*`，且需自行處理軟刪除轉換）——較不完整。

#### R5 no-op 判定（FR-008）
- **決定**：判斷「是否有實質變更」時，比對 `entry.Properties.Where(p => p.IsModified)`，並**排除**稽核欄（`CreateDate`、`CreateUser`、`ModifyDate`、`ModifyUser`）。若排除後無任何屬性變更且非 `Removed` 轉態 → 視為 no-op，**不**產生紀錄。
- **理由**：`AuditableEntityInterceptor` 每次 Modified 都刷新 `ModifyDate`，若不排除，任何存檔都會被誤判為有變更。
- **替代方案**：逐屬性值相等比對（更重、且 `IsModified` 已足夠）。

#### R6 遮罩策略（FR-004 / FR-005 / clarify Q3）
- **決定**：以 `[SensitiveData]` 屬性標記需遮罩的**憑證/機密**欄位（如 `User.PasswordHash`、`User.PasswordSalt`）。快照建置時，被標記的屬性值以 `"***"` 取代後才序列化。**一般個資（帳號、姓名）照實記錄**（clarify Q3=A），以查詢 API 的存取控制保護，不遮罩。`[SensitiveData]` 定義於 SharedKernel（供 Core 實體標註）。
- **理由**：宣告式標記使新增機密欄位時「加標記即被涵蓋」（FR-005、SC-005），不需改核心流程；對齊 Principle XVII 對憑證/機密不落地的 MUST。個資照實維持稽核可追查性（本表受選單授權控管）。
- **替代方案**：欄位名黑名單字串比對（易漏/誤判）、全個資遮罩（clarify Q3=B，降低追查價值，已否決）。

#### R7 受稽核範圍與排除清單（FR-014 / clarify Q1）
- **決定**：預設稽核**所有** `AppDbContext` 映射的實體；以 `[NotAudited]`（實體類別層級屬性）宣告排除。`AuditLog` 本身**恆排除**（避免對稽核表再稽核造成遞迴）。判定邏輯集中於 `AuditScope`。
- **理由**：對齊 clarify Q1（全覆蓋＋可設定排除、宣告式維護、不改核心流程）。屬性為編譯期、就近、無設定漂移。
- **遞迴防護**：攔截器先把待處理 entry **物化為清單**再產生/加入 `AuditLog`，故新加入的稽核列不會被同輪迭代再次稽核；另於 `AuditScope` 明確排除 `AuditLog` 型別作為雙重保險。
- **替代方案**：外部設定檔清單（執行期字串比對、易漂移）——本模板規模用屬性更穩健，日後可擴充設定檔。

#### R8 快照序列化格式（FR-015 整列快照）
- **決定**：改前/改後各序列化為一段 JSON 字串（`System.Text.Json`），內容為「欄位名 → 值」的整列鍵值物件；Insert 之 `OldValues` 為 `null`、Delete 之 `NewValues` 為 `null`。強型別 Id/值物件取其底層原生值後再序列化。
- **理由**：clarify Q2=A（整列快照，單筆看全貌）；逐欄位差異可由改前/改後比對得出（規格假設）。`System.Text.Json` 為 .NET 內建，不新增相依。
- **替代方案**：每變更欄位一列（clarify 已否決）；二進位/自訂格式（不可讀、無益查詢）。

#### R9 查詢 API 與授權（US2 / FR-011 / FR-012 / clarify Q1、Q2）
- **決定**：單一唯讀端點 `GET /audit-logs`，支援篩選（`entityName`、`primaryKey`、`operatedBy`、`from`、`to`）與分頁（沿用既有 GitHub 風格 Link 標頭），依發生時間（`CreateDate`，2026-09-30 前為 `OperatedAt`）排序。授權 `Policies("MenuAccess:/admin/audit-logs")`，對應新增之「資料異動紀錄」選單項（route `/admin/audit-logs`）。無專屬前端頁面（clarify Q1=B）。
- **理由**：沿用 `GetUsersEndpoint`/`GetUsersQuery` 既有分頁與授權慣例（005 選單導向模型），最小面積達成 FR-011/012。`AuditLog` 欄位皆原生型別，LINQ 可直接翻譯（無 Vogen `.Value` 翻譯限制）。
- **替代方案**：兩個獨立端點（依資料 / 依操作者）——合為一個帶篩選的端點更精簡；專屬 UI（clarify 已延後）。
- ⚠️ 2026-10-02 查詢 API 已移除（Clarifications Session 2026-10-02）。

#### R10 種子與開箱即用
- **決定**：`SeedData` 新增「資料異動紀錄」選單項（route `/admin/audit-logs`）並授予種子管理角色（admin）其可見性，比照 005 Phase 7 開箱即用做法。
- **理由**：使 admin 登入後即可查詢異動紀錄，無需人工改資料表；與 005 選單導向授權一致。
- **替代方案**：不種可見性（admin 需人工設定才可查）——降低開箱即用性。
- ⚠️ 2026-10-02 起種子改為軟刪除此選單與其可見性。

#### R11 效能與保留（Principle XIV / FR-016）
- **決定**：主鍵 DB 產生（R3）避免 O(n) 掃表；查詢一律 `AsNoTracking` + 投影 + 分頁。`SysAuditLog` 永久保留，系統不自動清理；於發生時間 `CreateDate`（及常用篩選）建索引以支撐查詢。
- **理由**：寫入維持 ≤500ms p95；永久保留為 clarify Q3 決定之取捨，清理由維運另行執行（已記於原則符合性）。
- **替代方案**：有界保留/自動清理（使用者先前明確不採用）。

#### R12 五個標準欄位（2026-09-30 修訂；DATABASE-SCHEMA §1、§6、§8；Clarifications 2026-09-30）
- **決定**：
  - `OperatedBy`／`OperatedAt` 改為 `CreateUser`／`CreateDate`，新增 `ModifyDate`／`ModifyUser`（與 Create* 同值）與 `Removed`（恆為 0）；`AuditLog` 實作 `IAuditable`、`ISoftDelete`，套用軟刪除全域過濾器。僅保留 `bigint IDENTITY` 主鍵一項例外（R3）。
  - 四個審計欄位由 `AuditLogInterceptor` 以既有的 `ICurrentUser.UserName`／`DateTime.Now` 傳入 `AuditLog` 建構子一次填入：該攔截器登記在 `AuditableEntityInterceptor` 之後（R4），新增的 `AuditLog` 列不會再經過後者。
  - Migration 以「改名」保留既有紀錄，`Modify*` 以 `Create*` 回填；兩個索引改為 EF 預設命名。
  - 查詢 API 對外的 `operatedBy`（參數與回應）、`operatedAt`（回應）名稱不變，由 `CreateUser`／`CreateDate` 投影；排序與期間篩選改比對 `CreateDate`。
- **理由**：所有表格式統一（使用者 2026-09-30 裁決）；做法與 SPEC-013 `SysUserLog` 一致。API 名稱是業務語意，與資料庫欄位名分開，維持不變可避免前端改版。
- **替代方案**：
  - 讓 `AuditableEntityInterceptor` 填入（調整攔截器順序或再跑一次）：順序是 R4 刻意設計（快照需含剛填入的 `Modify*`），不應為此改動。
  - API 一併改名為 `createUser`／`createDate`：屬破壞性變更，且欄位名對查詢者不直觀；無必要。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)（當時 v1.32.0）：初次檢查與設計後複查皆 PASS。Clean Architecture（`AuditLog` POCO 於 Core、攔截器於 Infrastructure）、DDD（`AuditOperation` 用 SmartEnum）、CQRS（寫入捕捉屬 Infrastructure 橫切攔截器，非命令）、測試先行與護欄（遮罩護欄、同交易回滾、授權矩陣）、zh-TW 註解與監控關鍵字 `[DATA_AUDIT]`、每欄 `.HasComment`、未新增第三方套件、機密不落地、Principle IX（前端）不適用。適用憲章 v1.32.0「append-only 稽核表例外」，例外紀錄（原 Complexity Tracking）：

| 取捨 | 為何需要 | 較簡單方案被否原因 |
|------|---------|-------------------|
| `AuditLog` 用 DB `bigint IDENTITY` 主鍵，非 Vogen 客戶端 ValueGenerator（適用 Principle VII append-only 例外） | 既有 `MaxIdCalculator` 每次寫入把整表載入記憶體算 `Max+1`（O(n)）；稽核表**永久保留**（FR-016）且**每次資料變更都寫入**，O(n) 掃表在成長後不可行。IDENTITY 為 O(1)、並發安全，且稽核表為 append-only、不被其他聚合以強型別 FK 參考 | Vogen+Max+1＝寫入路徑無界記憶體/延遲成長；Vogen+IDENTITY＝憲章明載 GetHashCode 執行期例外風險。故此技術表採原生 `long`+IDENTITY |
| `AuditLog` 不繼承 `AuditableEntityBase`（改為直接實作 `IAuditable`、`ISoftDelete`；2026-09-30 修訂） | `AuditableEntityBase<TId>` 綁定 Vogen 強型別 ID，與上列原生 `long` 主鍵不相容；五個標準欄位依 DATABASE-SCHEMA §1、§6 照常具備（設計決策 R12） | 繼承基底＝須改用 Vogen ID，回到上列否決的方案 |
| 排除清單以 `[NotAudited]` 屬性（宣告式）維護，而非設定檔 | 對齊 clarify Q1「宣告/設定式維護、變更範圍不改核心流程」；屬性為編譯期、就近、無設定漂移 | 外部設定檔＝執行期字串比對、易漂移；本模板規模用屬性更穩健（日後要設定檔可再擴充） |

2026-09-30 修訂：Principle XI 例外取消（五欄照常具備），僅 VII（主鍵 `bigint IDENTITY`）仍適用 DATABASE-SCHEMA §6 例外。

## Test Plan（測試計畫）

### 自動化測試

- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/Auditing/AuditScopeTests.cs`：受稽核範圍（AC-9）。
- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/Auditing/AuditSnapshotBuilderTests.cs`：操作分類、no-op、序列化與遮罩值（AC-1～AC-4、AC-10）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AuditLogInterceptorTests.cs`：新增／修改／刪除紀錄、五欄填入、回滾無孤兒、no-op（AC-1～AC-3、AC-5、AC-10、AC-11）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AuditRedactionTests.cs`：密碼雜湊與 salt 遮罩護欄（AC-4，FR-013）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AuditLogSchemaTests.cs`：`SysAuditLog` 綱要（AC-11）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/CurrentUserResolutionTests.cs`：已登入請求記錄真實帳號（AC-11）。
- [x] 移除驗證：`backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbMenuTreeSeedTests.cs`、`backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs`（AC-13）。

執行（範例）：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.UnitTests -- -class "TcmClinic.Api.UnitTests.Auditing.AuditSnapshotBuilderTests"
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests -- -class "TcmClinic.Api.IntegrationTests.Auditing.AuditLogInterceptorTests"
```

### 手動走查

前置：.NET 10 SDK、Docker（整合/功能測試用 Testcontainers）；以 Aspire 啟動或直接對 `TcmClinic.Api.Web` 執行。

1. 套用遷移：`dotnet ef database update --project backend/src/TcmClinic.Api.Infrastructure --startup-project backend/src/TcmClinic.Api.Web`。**預期**：資料庫出現 `SysAuditLog`，每欄與資料表皆有 zh-TW 描述（SSMS 可見 `MS_Description`）。
2. 執行捕捉相關自動化測試（見上），預期涵蓋：新增使用者 → 1 筆 `Insert`、`OldValues=null`、`PrimaryKey` 為該使用者 Id；修改顯示名稱 → 1 筆 `Update`；刪除（軟刪除）→ 1 筆 `Delete`；設定密碼 → `PasswordHash`/`PasswordSalt` 為 `"***"`；存檔失敗回滾 → 無孤兒列；未實質變更之存檔 → 不產生紀錄。
3. 手動煙霧測試：以種子 admin 登入，修改一位使用者的顯示名稱。
4. 由資料庫直接查閱 `SysAuditLog`（`EntityName = 'User'` 且 `PrimaryKey` 為該使用者 Id）。原步驟為呼叫 `GET /audit-logs?entityName=User&primaryKey={id}`，該 API 已於 2026-10-02 移除。
5. **預期**：見到一筆 `Update`，`NewValues` 含新顯示名稱、`CreateUser = admin`、`CreateDate` 為剛才時間；密碼相關欄位為 `"***"`。

原驗證對照：捕捉正確、主鍵正確 → FR-001/002/003/007、SC-001；遮罩 → FR-004、SC-002；同交易/回滾無孤兒 → FR-006、SC-004；查詢與授權 → FR-011/012、SC-003（已停止適用）；範圍/排除 → FR-014、SC-005。

## Dependencies（相依）

- SPEC-002 後台選單與角色權限管理：既有稽核欄位機制（`AuditableEntityBase`、`AuditableEntityInterceptor`，其設計決策 R6）。
- 既有「當前操作者」抽象 `ICurrentUser`（操作者來源，FR-009）。
- SPEC-004 使用者管理與登入：`User` 實體的密碼雜湊／salt 欄位（初始 `[SensitiveData]` 標註對象）。
- SPEC-005 選單導向授權模型：原查詢 API 授權（已移除）。
- 被依賴：SPEC-010 資料訪問日誌（沿用 `[SensitiveData]` 與稽核表例外）、SPEC-013 個人資料維護、SPEC-016 系統參數維護（參數修改前後值經本機制記錄）。

## Risks（風險）

- `SysAuditLog` 永久保留、系統不自動清理（FR-016），資料庫會持續成長；清理須由維運另行執行。
- 查詢 API 已移除，目前需由資料庫直接查閱異動紀錄；日後如需查詢畫面須另以規格定義。
- 主鍵採 `bigint IDENTITY`、不繼承 `AuditableEntityBase`，屬憲章 append-only 稽核表例外（見原則符合性）。
- 攔截器順序（R4）為正確性前提：若日後調整 `AuditableEntityInterceptor` 與 `AuditLogInterceptor` 的登記順序，快照完整性與五欄填入可能失效。
- 一般個資照實記錄（不遮罩），原以查詢 API 存取控制保護；查詢 API 移除後改由資料庫存取權限保護。

## 實作紀錄（Archive）

- **狀態**：已實作並收斂，所有任務完成（T001–T030）。
- **主要提交**：
  - 規格與設計：`007050e` 建立規格（spec/clarify/plan/契約）→ `ac55156` 任務清單 → `1d3c752` 對齊憲章 v1.32.0 append-only 例外。
  - 實作：`2596d34` 捕捉＋遮罩（US1）與查詢 API（US2）→ `9177040` 合併（Unit 51、Integration 18、Functional 47 全綠）。
  - 2026-09-30 資料表規範對齊：`2f630d5` 規格 → `32d03e6` 收斂任務 T027–T030 → `6000ead` 先失敗測試 → `24a5704` 實作 → `86f4781` 完成（全套 179 單元 + 81 整合 + 204 功能綠）。
  - `fa91926`（SPEC-016）：查詢清單預設每頁筆數改取系統參數。
  - `c8e5872`：2026-10-02 移除查詢 API 與「資料異動紀錄」選單。
- **審查紀錄**：規格品質檢查（原 `checklists/requirements.md`）全數通過；已定案 FR-014 範圍＝全覆蓋＋排除清單、FR-015 粒度＝整列快照、FR-016 保留＝永久保留、人工清理。
- **原 Spec Kit 產出**：plan、research、data-model、quickstart、tasks、checklists、contracts（audit-capture、audit-query-api）已併入本檔後移除；原始檔可由 `git show c8e5872:"specs/其他/資料表異動紀錄/tasks.md"` 等方式追溯。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：2026-09-30 資料表規範對齊修訂的紅燈 commit：`6e1bf33`（T027）。初版實作沒有單獨提交紅燈 commit，無紀錄可補。
