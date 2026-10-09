# [SPEC-010] Feature: 資料訪問日誌（Access Log）

<!-- spec-type: infrastructure -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-010 |
| 規格類型 | infrastructure |
| 狀態 | Archived |
| 選單 | 無（跨選單基礎規格）；原「資料訪問紀錄」選單 `/admin/access-logs` 已於 2026-10-02 移除 |
| 建立日期 | 2026-09-08 |
| 最後更新 | 2026-10-04（由 Spec Kit 格式轉為 UDS SDD 格式；內容未變） |
| 原分支 | `010-auth-access-logs` |
| 相關規格 | SPEC-006 資料表異動紀錄（沿用 `@SensitiveData` 宣告式標記與 append-only 稽核表例外）、SPEC-005 選單導向授權模型（原查詢 API 授權）、SPEC-004 使用者管理與登入（代表性示範掛載點 `GET /users`）、SPEC-013 個人資料維護（設計決策 R7 比照其 `SysUserLog` 做法）、SPEC-007 CHB選單與角色種子（2026-10-02 舊後台選單移除）；⚠️ 部分被 SPEC-016 系統參數維護修改（訪問日誌清單預設每頁筆數改取「查詢列表分頁筆數」，其 FR-008(b)，初始值 10，原 20；查詢 API 移除後該列停止適用）；日後查詢畫面由 SPEC-060 使用者存取記錄依舊系統重新定義；認證日誌 `SysAuthLog` 延後另開 |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

> **狀態沿革**：Implemented（2026-09-08）；2026-09-30 修訂已完成（資料表規範對齊，見 Clarifications Session 2026-09-30）；2026-10-02 移除舊後台選單（見 Clarifications Session 2026-10-02）。
>
> **註**：分支名保留 `auth-access-logs`（建立時的原始構想含認證日誌）；經澄清後，認證事件日誌（`SysAuthLog`）已**移出本次範圍**（見 Clarifications 與範圍外），本規格僅交付**資料訪問日誌（`SysAccessLog`）**。
>
> ⚠️ **2026-10-02**：查詢 API（US2）與「資料訪問紀錄」選單已移除，訪問紀錄的寫入不變；US2、FR-011～FR-013 及 FR-015 中「唯讀查詢 API」停止適用。
>
> ⚠️ **部分被 SPEC-016 系統參數維護修改**：訪問日誌清單預設每頁筆數改取系統參數「查詢列表分頁筆數」（系統參數維護 FR-008(b)，初始 10；原預設 20）。查詢 API 移除後，該列停止適用。2026-10-06 SPEC-016 再移除該參數（[ADR-007](../架構決策/ADR-007-default-page-size-fixed.md)），本規格不受實際影響。

## Overview（概述）

原始需求：「幫我新增這些你講的 Log 的功能吧，……命名建議按照你說的方式去弄，記得資料表每個欄位要有中文說明」。本規格新增 append-only 日誌表 `SysAccessLog`，在使用者查詢、匯出或下載**涉及敏感資料**的資料時，以旁路、盡力寫入的方式留下訪問紀錄。紀錄只寫入被訪問的敏感**欄位名**，不寫入機密值。交付內容是一套可重用的記錄能力（資料表、記錄服務、機密不入庫的護欄測試），並以 `GET /users` 作為代表性示範掛載點，證明端到端可以運作。原本的唯讀查詢 API 已於 2026-10-02 移除。

## Motivation（動機）

### 背景與定位（Context）

專案已在 SPEC-006（`006-data-audit-trail`）交付「資料異動紀錄」（`SysAuditLog`），以 EF Core 攔截器自動捕捉受稽核資料表的「改前/改後」整列快照——**只記錄資料的新增／修改／刪除（寫入）**。

有一類重要事件不屬於「資料異動」，目前無專屬記錄：**敏感資料的訪問**（查詢、匯出、下載）。`SysAuditLog` 刻意不記錄純讀取，但隱私合規常要求能追查「誰查了／匯出了敏感資料」。

本規格新增一張 append-only 日誌表 `SysAccessLog`，沿用 SPEC-006 的稽核表例外模式（`bigint IDENTITY` 主鍵、每欄與資料表皆附 zh-TW `.HasComment`；2026-09-30 起與一般表相同具備五個標準欄位，見 Clarifications Session 2026-09-30），並提供受選單授權控管的最小唯讀查詢 API（2026-10-02 移除）。命名沿用專案既有 `Sys` 前綴、PascalCase 慣例（`SysUser`／`SysRoles`／`SysAuditLog`）。

### 優先序理由

- **US1（P1）**：訪問（讀取）日誌是隱私合規的核心要求，也是 `SysAuditLog`（只記寫入）刻意留下的缺口。這是本規格的價值主體，且與登入內部無關，可獨立穩定交付。即使沒有任何檢視介面，訪問日誌本身即具備合規稽核價值。
- **US2（P2，已移除）**：讓已累積的訪問日誌可被人使用；但在缺少此介面時，紀錄仍可經由資料層直接查閱，故次於捕捉本身。與 SPEC-006 的查詢能力一致，沿用相同的選單導向授權模型（SPEC-005）。

## Clarifications（釐清紀錄）

### Session 2026-09-08

- Q: 在 Cookie 驗證、無伺服器端 session 存儲、目前無 MFA 的現況下，`SysAuthLog` 的會話與 MFA 欄位如何設計？ → A: 登入為臨時實作、規格尚未定案，本次不將日誌綁定登入內部細節。
- Q: 既然登入規格未定案，本次 feature 對 `SysAuthLog`（認證日誌）如何處理？ → A: 本次**只交付 `SysAccessLog`（資料訪問日誌）＋共用唯讀查詢 API＋基礎設施**；`SysAuthLog` 整組**延後**至登入定案後另開 feature（範圍由兩表縮為一表）。
- Q: 在無匯出／下載端點、讀取 DTO 不含機密值的現況下，`SysAccessLog` 要交付到什麼程度？ → A: 交付**可重用的訪問記錄能力**（表＋記錄服務／抽象＋查詢 API＋機密不入庫的護欄測試），並以**一個代表性示範掛載點**端到端證明可運作；**不掃改**現有端點。

### Session 2026-09-30

背景：DATABASE-SCHEMA §1、§6 修訂為「所有表（含 append-only 稽核／日誌表）統一五個標準欄位」，本表原以自訂欄位記錄訪問者與時間、且免除軟刪除與最後修改欄，不符新規範。

- 訪問紀錄改用系統統一的五個標準欄位記錄訪問者與時間（建立者＝訪問者、建立時間＝訪問時間；修改者／修改時間與其同值；刪除旗標恆為否），並與一般表相同套用軟刪除查詢過濾；不另設自訂欄位。既有紀錄保留並轉換，不遺失。僅資料庫產生的主鍵維持例外。
- 查詢 API 對外欄位與篩選參數名稱（訪問者、訪問時間）維持不變，前端不需改版；外部可觀察行為不變。

### Session 2026-10-02

背景：CHB 選單已併入規格（見 CHB選單與角色種子），範本早期的舊後台選單（`/admin/users`、`/admin/roles`、`/admin/audit-logs`、`/admin/access-logs`）不再使用。使用者決定直接移除並修訂相關規格，不另開規格。

- Q: 訪問日誌查詢 API 與「資料訪問紀錄」選單如何處理？ → A: **移除**查詢 API（`GET /access-logs`）與「資料訪問紀錄」選單（`/admin/access-logs`，種子軟刪除）；訪問紀錄的寫入（User Story 1）與記錄服務不變。User Story 2、FR-011～FR-013 與 FR-015 中「唯讀查詢 API」停止適用，目前需要時由資料庫直接查閱；日後如需查詢畫面，由其他規格（如 使用者存取記錄）依舊系統重新定義。

## Requirements（需求）

### Requirement: US1 記錄敏感資料的訪問（查詢／匯出／下載）（P1）

The system SHALL 在使用者查詢、匯出或下載涉及敏感資料時，於 `SysAccessLog` 以 append-only 方式產生一筆只含敏感欄位名、不含機密值的訪問紀錄。

當使用者查詢、匯出或下載**涉及敏感資料**的資料時，系統必須於 `SysAccessLog` 產生一筆訪問紀錄，保存訪問類型、目標實體與主鍵、涉及筆數、訪問者、來源 IP、是否涉及敏感資料、被訪問的敏感欄位清單、訪問目的（若有）與訪問時間，以支援隱私合規與敏感資料保護稽核。此為本功能的 MVP。涵蓋 FR-001～FR-010、FR-014～FR-016。

**獨立測試**：對含敏感欄位的資料執行一次查詢、一次匯出，檢查 `SysAccessLog`，確認各留下一筆紀錄，含訪問類型、目標實體與主鍵、涉及筆數、訪問者與時間，且敏感欄位清單正確；再對不含敏感欄位的一般查詢，確認不產生訪問紀錄（避免爆量）；並修改含機密欄位的資料訪問情境，確認紀錄中不含任何密碼／雜湊／token。

#### Scenario: AC-1 敏感資料查詢產生 Read 紀錄
- **GIVEN** 一筆含敏感欄位的資料
- **WHEN** 使用者查詢並檢視該筆資料
- **THEN** 系統產生一筆 `AccessType=Read`、`InvolvesSensitiveData=true` 的訪問紀錄，含目標實體、主鍵、訪問者與時間，並列出被訪問的敏感欄位

#### Scenario: AC-2 匯出／下載產生 Export／Download 紀錄
- **GIVEN** 一份資料清單
- **WHEN** 使用者匯出或下載
- **THEN** 系統產生一筆 `AccessType=Export`（或 `Download`）紀錄，記錄涉及筆數（`RecordCount`）與訪問者

#### Scenario: AC-3 一般查詢不記錄
- **GIVEN** 一般不含敏感欄位的查詢
- **WHEN** 使用者執行查詢
- **THEN** 系統**不**產生訪問紀錄（避免日誌爆量）

#### Scenario: AC-4 機密內容不入紀錄
- **GIVEN** 一筆含機密欄位（如密碼雜湊）的資料被訪問
- **WHEN** 產生訪問紀錄
- **THEN** 紀錄中不得出現密碼、雜湊、token、cookie、連線字串等機密內容

#### Scenario: AC-5 紀錄僅供新增
- **GIVEN** 訪問事件寫入
- **WHEN** 任何情況下
- **THEN** 紀錄一經產生即為僅供新增（append-only），一般操作路徑不得竄改或刪除

### Requirement: US2 查詢資料訪問日誌（最小唯讀 API，受選單授權控管）（P2）（⚠️ 2026-10-02 移除，停止適用；見 Clarifications Session 2026-10-02）

The system SHALL（2026-10-02 起停止適用）提供受選單授權控管的最小唯讀查詢 API，讓具權限者依篩選條件分頁查詢訪問紀錄。

具權限的管理／稽核人員能透過**最小的唯讀查詢 API**，依篩選條件查詢資料訪問紀錄：可依「訪問者＋期間」「目標實體＋主鍵」「是否涉及敏感資料」查詢，結果依時間由新到舊、分頁。本次**不含專屬前端檢視頁面**（可留待日後 feature）。涵蓋 FR-011～FR-013。

**獨立測試**：累積數筆訪問事件後，透過查詢 API 以不同篩選條件取得結果，確認排序（新到舊）、分頁與篩選正確；並以未具稽核權限者存取，確認被拒絕。

#### Scenario: AC-6 依目標實體＋主鍵查詢訪問歷史
- **GIVEN** 某筆敏感資料曾被多次訪問
- **WHEN** 稽核人員以「目標實體＋主鍵」查詢訪問日誌
- **THEN** 系統回傳該筆資料依時間由新到舊、分頁的訪問歷史

#### Scenario: AC-7 依訪問者＋期間查詢
- **GIVEN** 系統中累積若干訪問事件
- **WHEN** 稽核人員以「訪問者＋期間」查詢
- **THEN** 系統只回傳該訪問者於該期間的訪問紀錄

#### Scenario: AC-8 篩選敏感訪問
- **GIVEN** 稽核人員想聚焦敏感訪問
- **WHEN** 以「是否涉及敏感資料＝是」篩選
- **THEN** 系統只回傳涉及敏感資料的訪問紀錄

#### Scenario: AC-9 未具權限者被拒
- **GIVEN** 一位未具稽核權限的使用者
- **WHEN** 其嘗試查詢訪問日誌
- **THEN** 系統拒絕存取

### 功能需求條文（Functional Requirements）

#### 通用（本表基礎）

- **FR-001**: 系統 MUST 新增一張 append-only 日誌表 `SysAccessLog`（資料訪問日誌），命名沿用專案 `Sys` 前綴／PascalCase 慣例。
- **FR-002**: 本表 MUST 依循 DATABASE-SCHEMA §6 的「append-only 稽核表例外」：以資料庫產生的 `bigint IDENTITY` 為主鍵；§1 五個標準欄位照常具備——以 `CreateUser`／`CreateDate` 記錄「誰／何時」，`ModifyUser`／`ModifyDate` 與其同值，`Removed` 恆為否，並與一般表相同套用軟刪除全域查詢過濾器（2026-09-30 修訂）。
- **FR-003**: 本表的**每一個欄位**與**資料表本身** MUST 附帶繁體中文（zh-TW）說明，並輸出至資料庫（等同 EF Core `.HasComment`／SQL Server `MS_Description`），使 schema 在資料庫工具中自我說明。缺少或不正確的欄位說明視為缺陷，須於審查阻擋。
- **FR-004**: 本表的紀錄 MUST 為僅供新增（append-only）：一經產生，一般操作路徑不得更新或刪除既有紀錄。
- **FR-005**: 本表 MUST NOT 寫入被訪問資料的憑證／機密**值**（密碼、密碼雜湊、salt、token、cookie、完整連線字串等）；訪問日誌僅記錄「被訪問的敏感**欄位名**」清單，對齊 SPEC-006 的遮罩策略與憲章 Principle XVII。
- **FR-006**: 本日誌 MUST 永久保留；系統本身不自動清理，清理由維運另行執行。此策略 MUST 記錄為已知的磁碟成長取捨。
- **FR-007**: 訪問者來源 MUST 沿用系統既有的「當前操作者」機制（`ICurrentUser`）；在系統背景作業情境下 MUST 記錄可識別的系統操作者標識。

#### 資料訪問日誌（SysAccessLog）

- **FR-008**: 系統 MUST 在使用者對**涉及敏感資料**的訪問行為（查詢／匯出／下載）發生時，於 `SysAccessLog` 產生一筆訪問紀錄；匯出／下載 MUST 一律記錄，一般查詢僅在涉及敏感欄位時記錄，避免日誌爆量。
- **FR-009**: 每筆訪問紀錄 MUST 至少包含：訪問類型（查詢／匯出／下載）、目標實體名稱、目標主鍵、涉及筆數、訪問者、來源 IP、是否涉及敏感資料、被訪問的敏感欄位名清單、訪問目的（若有）與訪問時間。
- **FR-010**: 「是否涉及敏感資料／敏感欄位清單」的判定 MUST 沿用 SPEC-006 既有的宣告式敏感欄位標記，新增敏感欄位時無需改動訪問記錄流程。

#### 查詢 API 與授權

- **FR-011**: 具權限者 MUST 能透過最小唯讀查詢 API 查詢訪問日誌（可依「訪問者＋期間」「目標實體＋主鍵」「是否涉及敏感資料」篩選），結果依時間由新到舊並分頁。（⚠️ 2026-10-02 移除，停止適用；見 Clarifications Session 2026-10-02）
- **FR-012**: 查詢 API 的分頁 MUST 強制最大頁面大小（預設上限 100 筆），不得回傳無界結果集（對齊憲章 Principle XIV）。（⚠️ 2026-10-02 移除，停止適用；見 Clarifications Session 2026-10-02）
- **FR-013**: 查詢日誌的能力 MUST 受既有選單導向授權模型（SPEC-005）控管——透過受保護的功能項（「資料訪問紀錄」），僅可見（＝可操作）該功能項的角色能存取；未具權限者不得存取，且不另建平行授權機制。（⚠️ 2026-10-02 移除，停止適用；見 Clarifications Session 2026-10-02）
- **FR-014**: 日誌寫入 MUST NOT 因非致命錯誤而中斷或拖垮使用者主流程（查詢／匯出）；日誌為旁路記錄，以「盡力寫入」為原則，若寫入失敗須留下可觀測的系統告警。此容錯策略 MUST 記錄為已知取捨，並與 SPEC-006「異動紀錄與資料變更同交易」的強一致策略刻意區隔（因訪問日誌失敗不應阻擋使用者讀取資料）。

#### 交付範圍

- **FR-015**: 本次 MUST 交付一個**可重用的訪問記錄能力**——即 `SysAccessLog` 資料表、供呼叫端記錄訪問事件的記錄服務／抽象、唯讀查詢 API，以及確保機密值不入庫的護欄測試；此能力供未來敏感讀取／匯出端點呼叫。（⚠️ 2026-10-02：其中「唯讀查詢 API」已移除，見 Clarifications Session 2026-10-02）
- **FR-016**: 本次 MUST 以**至少一個代表性示範掛載點**（指定一個敏感讀取或匯出情境）將上述能力端到端接通並證明可運作，作為 SC-001／SC-002 的驗收載體。系統 MUST NOT 為此 feature 掃改（retrofit）所有現有讀取端點；既有端點的逐一接入屬各自後續工作。

#### 成功標準（Success Criteria）

- **SC-001**: 在代表性示範掛載點（FR-016）上，對涉及敏感資料的匯出／下載，100% 於 `SysAccessLog` 留下一筆含正確涉及筆數（`RecordCount`）的紀錄；對不含敏感欄位的一般查詢，產生的訪問紀錄數為 0（避免爆量）。
- **SC-002**: 訪問日誌中，憑證／機密**值**（密碼、雜湊、token 等）出現的比率為 0%（由護欄測試持續驗證）。
- **SC-003**: 稽核人員能以「目標實體＋主鍵」或「訪問者＋期間」在單次查詢內取得依時間排序、分頁的訪問歷史。（⚠️ 2026-10-02 查詢 API 移除後停止適用）
- **SC-004**: `SysAccessLog` 的每一個欄位與資料表本身，在資料庫中皆可查得繁體中文說明（覆蓋率 100%）。
- **SC-005**: 訪問日誌寫入的非致命失敗不影響使用者查詢／匯出主流程的成功率（主流程不因日誌問題而失敗）。

### 邊界情境（Edge Cases）

- **機密絕不入庫**：任何訪問紀錄都不得寫入被訪問資料的密碼、密碼雜湊、salt、token、cookie、連線字串（對齊憲章 Principle XVII 與 SPEC-006 的遮罩策略）；訪問日誌記錄的是「哪些敏感欄位被訪問」的**欄位名清單**，而非其值。
- **來源 IP 取得失敗**：當代理／負載平衡導致無法取得可靠來源 IP 時，IP 欄位允許為空，事件仍須記錄。
- **匯出大量筆數**：匯出／下載以「一次操作一筆紀錄」記錄，並以 `RecordCount` 表示涉及筆數，不逐筆產生訪問紀錄。
- **敏感欄位清單日後擴充**：新增敏感欄位時，訪問日誌的「是否涉及敏感資料／敏感欄位清單」判定須沿用既有的宣告式敏感標記（對齊 SPEC-006），無需逐一改動記錄流程。
- **日誌寫入不得拖垮主流程**：訪問日誌寫入失敗時的處置策略須明確（見 FR-014），避免因日誌問題導致使用者無法查詢或匯出。
- **一般查詢不記錄**：不涉及任何敏感欄位的查詢一律不產生訪問紀錄，避免日誌爆量；此判定須可由宣告式敏感標記自動得出，而非逐一端點手動判斷。

### 關鍵實體（Key Entities）

- **資料訪問紀錄（Access Log / `SysAccessLog`）**：代表一次對敏感資料的讀取／匯出／下載事件。關鍵屬性：主鍵（資料庫產生流水號）、訪問類型（查詢／匯出／下載）、目標實體名稱、目標主鍵、涉及筆數、訪問者、來源 IP、是否涉及敏感資料、被訪問的敏感欄位名清單、訪問目的、訪問時間。與被訪問資料為「多對一」的邏輯關聯（透過實體名稱＋主鍵），本身不修改被訪問的資料。
- **敏感欄位標記（Sensitive Field Marker）**：沿用 SPEC-006 對實體欄位的宣告式標註；於訪問日誌用以判定「是否涉及敏感資料」、產生被訪問的敏感欄位名清單，並確保機密值不入庫。

### 假設（Assumptions）

- 沿用專案既有的「當前操作者」抽象（`ICurrentUser`）作為訪問者來源，並沿用 SPEC-006 的宣告式敏感欄位標記機制。
- 命名沿用你先前建議：`SysAccessLog`；沿用 `Sys` 前綴、PascalCase、`bigint IDENTITY` 主鍵、每欄 zh-TW `.HasComment` 的既有慣例。
- **訪問日誌的範圍**採「敏感導向」：匯出／下載一律記錄，一般查詢僅在涉及敏感欄位時記錄，以避免日誌爆量；全量讀取日誌不在本次範圍。
- 訪問日誌與被稽核資料存放於同一資料庫；訪問日誌屬「旁路記錄」，以「盡力寫入」為原則（FR-014），與 SPEC-006 資料異動紀錄「與資料變更同交易」的強一致策略刻意不同——因訪問日誌失敗不應阻擋使用者讀取資料。
- 個資（帳號、使用者名稱）在此**受權限控管的日誌表**中照實記錄（以存取控制取代遮罩），僅確保機密**值**不入庫——與 SPEC-006 一致。
- 查詢能力本次以「最小唯讀查詢 API（受選單授權控管）」交付，不含專屬前端檢視頁面；日後如需 UI 另開 feature。（⚠️ 2026-10-02 查詢 API 已移除）
- 授權沿用既有選單導向模型（SPEC-005），新增「資料訪問紀錄」受保護功能項，不另建平行授權機制。（⚠️ 2026-10-02 該功能項已移除）
- 保留策略為「永久保留、由維運另行清理」，與 SPEC-006 一致（已知取捨：資料庫會持續成長）。
- 效能預算沿用憲章 Principle XIV 預設：查詢日誌 API ≤ 300ms p95；日誌寫入為旁路、不得使被稽核的查詢／匯出主流程逾越其既有預算。
- **認證事件日誌（`SysAuthLog`）延後**（出口：specs/README.md 延後事項 DEF-005）：因登入規格未定案，本次不實作；分支名保留 `auth-access-logs` 僅為歷史原因。

### 範圍外（Out of Scope）

- **認證事件日誌（`SysAuthLog`）**：登入／登出／登入失敗等認證事件的記錄，因登入規格尚未定案（現行登入為臨時實作）而**延後**，待登入定案後另開 feature 一併設計（屆時再決定會話識別、會話時長回推、MFA、來源 IP 等欄位）。
- **專屬前端檢視頁面**：本次僅交付受權限控管的最小唯讀查詢 API，不含 UI。
- **全量讀取日誌**：不記錄所有讀取，只記錄涉及敏感資料的訪問（及一律記錄的匯出／下載）。
- **掃改所有現有端點**：本次不 retrofit 既有讀取端點；僅交付可重用能力＋一個代表性示範掛載點（FR-015／FR-016），既有端點逐一接入屬後續工作。

## Acceptance Criteria（驗收條件）

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、SC-001、FR-008、FR-009 | 讀取含 `@SensitiveData` 欄位的實體時產生一筆 Read 紀錄，敏感欄位清單正確 | `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AccessLogSchemaTests.cs` › `Recorded_access_log_has_standard_columns_filled`（記錄服務對 `User` 的 Read 寫入一筆）；`backend/tests/TcmClinic.Api.UnitTests/Auditing/SensitiveFieldDetectorTests.cs` › `Detects_user_sensitive_fields`（清單含 `PasswordHash`、`PasswordSalt`）；手動：Test Plan 手動走查第 2～3 步 |
| AC-2 | US1 情境 2、SC-001（匯出／下載） | 匯出／下載產生一筆 Export／Download 紀錄並記 `RecordCount` | 【延後】目前無匯出／下載端點（T009，見實作紀錄「延後與作廢事項」）；手動：Code Review 確認 `AccessLogRecorder` 只對 Read 做敏感判定、Export／Download 一律寫入 |
| AC-3 | US1 情境 3、SC-001（不爆量）、FR-010 | 不含敏感欄位的一般查詢不產生紀錄 | `SensitiveFieldDetectorTests` › `Plain_type_has_no_sensitive_fields`；手動：Code Review 確認 `AccessLogRecorder` 在 `AccessType.Read` 且無敏感欄位時直接返回 |
| AC-4 | US1 情境 4、SC-002、FR-005 | 紀錄只含敏感欄位名、不含任何機密值（出現率 0%） | `SensitiveFieldDetectorTests` › `Returns_field_names_never_values`；手動：Test Plan 手動走查第 4 步 |
| AC-5 | US1 情境 5、FR-004 | 紀錄為 append-only，一般操作路徑不得更新或刪除 | 手動：Code Review 確認 `AccessLog` 業務欄位為 private setter、無更新／刪除方法，程式中無對 `AccessLogs` 的更新或刪除呼叫 |
| AC-6 | US2 情境 1、SC-003、FR-011 | 依「目標實體＋主鍵」查詢得新到舊分頁歷史 | 停止適用（2026-10-02）；原驗證測試 `GetAccessLogsQueryTests` 已隨 `c8e5872` 刪除；移除本身由 AC-14 驗證 |
| AC-7 | US2 情境 2、SC-003、FR-011 | 依「訪問者＋期間」查詢只回傳該訪問者該期間紀錄 | 停止適用（2026-10-02）；同 AC-6 |
| AC-8 | US2 情境 3、FR-011 | 「是否涉及敏感資料＝是」只回傳敏感紀錄 | 停止適用（2026-10-02）；同 AC-6 |
| AC-9 | US2 情境 4、FR-013 | 未具「資料訪問紀錄」權限者被拒 | 停止適用（2026-10-02）；原驗證測試 `GetAccessLogsAuthorizationTests` 已隨 `c8e5872` 刪除 |
| AC-10 | FR-003、SC-004 | `SysAccessLog` 每欄與資料表皆有 zh-TW 說明（覆蓋率 100%） | `AccessLogSchemaTests` › `SysAccessLog_schema_matches_design` |
| AC-11 | FR-002、FR-007、設計決策 R7 | 主鍵為 `bigint` identity；五個標準欄位齊備、由攔截器填入（建立者＝訪問者，背景作業為 `system`；Modify* 同 Create*；`Removed`＝false）；無 `AccessedBy`／`AccessedAt`；三個索引採 EF 預設命名 | `AccessLogSchemaTests` › `SysAccessLog_schema_matches_design`、`Recorded_access_log_has_standard_columns_filled` |
| AC-12 | FR-014、SC-005 | 日誌寫入失敗不拋出、不中斷主流程，並留 `[ACCESS_LOG]` 錯誤告警 | 手動：Code Review 確認 `AccessLogRecorder.RecordAsync` 以 try-catch 包覆、catch 內 `LogError("[ACCESS_LOG] …")` 且不重拋，並卸除未落庫的實體 |
| AC-13 | FR-016 | 代表性示範掛載點 `GET /users` 於回應前記錄一筆 Read 紀錄 | 手動：Test Plan 手動走查第 1～3 步（原端到端測試 `GetAccessLogsQueryTests` › `Reading_sensitive_entity_records_access_log` 已隨 `c8e5872` 刪除） |
| AC-14 | Clarifications Session 2026-10-02 | 「資料訪問紀錄」選單 `/admin/access-logs` 不再種入、`/me/menus` 不再出現 | `backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbMenuTreeSeedTests.cs` › `Admin_menus_keep_only_menu_management`；`backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs` › `Seeded_admin_sees_menus_and_can_access` |

> AC-6～AC-9（US2）與 SC-003 自 2026-10-02 起停止適用，查詢 API 與其測試已移除（見 Clarifications Session 2026-10-02）。
>
> AC-1 與 AC-13 的端到端自動驗證原由 `GetAccessLogsQueryTests` 透過查詢 API 讀回紀錄；該測試隨查詢 API 一併刪除後，目前只剩記錄服務層級的整合測試與手動走查。

## Technical Design（技術設計）

### 技術脈絡

- 語言／框架：C# 12 / .NET 8.0（Aspire）；EF Core 8.0（ORM、SaveChangesInterceptor）、FastEndpoints、Mediator（CQRS dispatch）、Vogen（強型別 ID）、SmartEnum（`AccessType`）。
- 儲存：SQL Server 2019+（既有專案 DB），新增 `SysAccessLog` 一張表。
- 測試：xUnit＋Testcontainers（MsSql）。
- 目標平台：ASP.NET Core 8.0 Web API（Aspire DCP 開發、Azure Deploy 正式環境）。
- 效能目標：原查詢日誌 API ≤300ms p95（Principle XIV，API 已移除）；訪問日誌寫入為旁路、盡力寫入，不拖垮主流程（≤500ms p95 的查詢／匯出端點）；表成長管理為永久保留、由維運清理。
- 限制：敏感欄位檢測沿用 SPEC-006 宣告式標記；日誌查詢授權透過 SPEC-005 選單導向模型（已移除）；代表性示範掛載點選定一個敏感讀取或匯出情境；訪問日誌為旁路記錄，失敗不阻擋主流程。
- 規模：單張表；原查詢 API 至多 4 種篩選維度（訪問者＋期間、目標實體＋主鍵、敏感資料篩選、權限拒絕）；代表性示範掛載點 1 個端點；機密護欄測試至少 3 個情境。

### 受影響元件

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.Core/Auditing/AccessLog.cs` | `SysAccessLog` 實體：`bigint` 主鍵、業務欄位、五個標準欄位（實作 `IAuditable`、`ISoftDelete`），標記 `[NotAudited]` 避免寫入時再產生 `SysAuditLog` |
| `backend/src/TcmClinic.Api.Core/Auditing/AccessType.cs` | SmartEnum：Read／Export／Download |
| `backend/src/TcmClinic.Api.Core/Auditing/IAccessLogRecorder.cs` | 記錄服務抽象 `IAccessLogRecorder` 與請求 `AccessLogRequest`（FR-015） |
| `backend/src/TcmClinic.Api.Core/Auditing/SensitiveFieldDetector.cs` | 依 `@SensitiveData` 反射取得敏感欄位名（FR-010） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Config/AccessLogConfig.cs` | EF 設定：每欄 zh-TW 說明、五個標準欄位、三個索引 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Auditing/AccessLogRecorder.cs` | 記錄服務實作：敏感判定、只序列化欄位名、盡力寫入與 `[ACCESS_LOG]` 告警 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/AppDbContext.cs` | 新增 `DbSet<AccessLog> AccessLogs` |
| `backend/src/TcmClinic.Api.Infrastructure/InfrastructureServiceExtensions.cs` | 註冊 `IAccessLogRecorder` → `AccessLogRecorder`（Scoped） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20260908082759_AddAccessLog.cs` | 建立 `SysAccessLog` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20261001033538_SysAccessLogStandardColumns.cs` | 2026-09-30 修訂：改名為五個標準欄位、回填、重建索引（設計決策 R7） |
| `backend/src/TcmClinic.Api.SharedKernel/ICurrentUser.cs` | 新增 `SourceIp`（預設 `null`） |
| `backend/src/TcmClinic.Api.Web/Services/HttpContextCurrentUser.cs` | 以 HttpContext 提供 `SourceIp` |
| `backend/src/TcmClinic.Api.Web/Endpoints/Users/GetUsersEndpoint.cs` | 代表性示範掛載點：回應前記錄一筆 Read（FR-016） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/SeedData.cs` | 2026-10-02：`/admin/access-logs` 列入已停用的舊後台選單，由種子軟刪除 |

2026-10-02 已刪除（`c8e5872`）：`TcmClinic.Api.Core/Auditing/Specifications/AccessLogSpecs.cs`、`TcmClinic.Api.UseCases/Auditing/GetAccessLogs.cs`、`TcmClinic.Api.UseCases/Auditing/AccessLogDto.cs`、`TcmClinic.Api.Web/Endpoints/Auditing/GetAccessLogsEndpoint.cs`，以及對應的功能測試。

### 資料模型

#### `SysAccessLog` — 資料訪問紀錄

用途：記錄使用者對敏感資料的讀取、匯出、下載事件，用於隱私合規與敏感資料保護稽核。

| # | 欄位名（英文） | 欄位名（中文） | 型別 | 長度 | NULL | 說明 |
|----|---|---|---|---|---|---|
| 1 | Id | 主鍵流水號 | `bigint` | — | NO | 資料庫 IDENTITY 產生；append-only 表例外（Principle VII） |
| 2 | AccessType | 訪問類型 | `nvarchar` | 50 | NO | 列舉值：Read（查詢）、Export（匯出）、Download（下載）；對應 AccessType SmartEnum |
| 3 | EntityName | 目標實體名稱 | `nvarchar` | 128 | NO | 被訪問的領域實體名稱（如「SysUser」、「Order」）；用於追蹤特定資源訪問 |
| 4 | PrimaryKey | 目標主鍵 | `nvarchar` | 256 | NO | 被訪問資料的主鍵值；單一鍵格式如「123」，複合鍵格式如「key1=v1;key2=v2」 |
| 5 | RecordCount | 涉及筆數 | `int` | — | NO | 本次訪問涉及的資料筆數；匯出/下載多筆時表示筆數，單筆讀取通常為 1 |
| 6 | ClientIp | 來源 IP 位址 | `nvarchar` | 45 | YES | 發起訪問的用戶端 IP（IPv4 或 IPv6）；支援代理頭解析（X-Forwarded-For）；背景作業為 NULL |
| 7 | InvolvesSensitiveData | 是否涉及敏感資料 | `bit` | — | NO | 布爾值；True 表示訪問包含 @SensitiveData 標記的欄位 |
| 8 | SensitiveFieldsAccessed | 敏感欄位清單 | `nvarchar` | max | YES | JSON 陣列格式，列舉被訪問的敏感欄位名稱（如 `["PasswordHash", "PasswordSalt"]`）；僅當 InvolvesSensitiveData=True 時填入 |
| 9 | Purpose | 訪問目的 | `nvarchar` | 500 | YES | 說明此次訪問的業務目的或原因（可選）；供稽核人員追溯 |
| 10 | CreateDate | 訪問時間 | `datetime` | — | NO | 訪問發生時的時間戳（本地時區）；由 `AuditableEntityInterceptor` 於新增時填入 |
| 11 | CreateUser | 訪問者 | `nvarchar` | 50 | NO | 執行訪問的使用者帳號（或「system」表示背景作業）；沿用 ICurrentUser.UserName，由攔截器填入 |
| 12 | ModifyDate | 最後修改時間 | `datetime` | — | NO | 與 `CreateDate` 同值（列不會被更新） |
| 13 | ModifyUser | 最後修改者 | `nvarchar` | 50 | NO | 與 `CreateUser` 同值（列不會被更新） |
| 14 | Removed | 軟刪除旗標 | `bit` | — | NO | 恆為 0（列不會被軟刪除）；預設 0 |

> **2026-09-30 修訂（設計決策 R7）**：原 `AccessedBy`／`AccessedAt` 改為 §1 五個標準欄位（上表 10–14），`AccessLog` 實作 `IAuditable`、`ISoftDelete`，與一般表相同套用軟刪除全域過濾器；僅 `bigint IDENTITY` 主鍵維持 §6 例外。原設計共 11 欄（`AccessedBy` nvarchar(50)、`AccessedAt` datetime 本地時區），現為 14 欄。

> **實作註記**（[Source: Code] `backend/src/TcmClinic.Api.Infrastructure/Data/Config/AccessLogConfig.cs`、`backend/src/TcmClinic.Api.Core/Auditing/AccessLog.cs`）：
> - `PrimaryKey` 實作為**可空**（匯出多筆時為 `null`），與原 API 契約 `primaryKey: string | null` 一致；上表「NO」為原設計值。
> - `EntityName` 實際寫入 C# 實體型別名（如 `User`），不是資料表名 `SysUser`。
> - `ClientIp` 實際只取連線的 `RemoteIpAddress`，未解析 `X-Forwarded-For`（見設計決策 R4 實作註記）。
> - `AccessType` 以 SmartEnum 的 `Name` 字串持久化。

#### 索引

| 索引名（EF 預設命名，2026-09-30 起） | 欄位 | 用途 |
|---|---|---|
| `IX_SysAccessLog_EntityName_PrimaryKey_CreateDate` | EntityName, PrimaryKey, CreateDate | 查詢特定資料（實體＋主鍵）的訪問歷史 |
| `IX_SysAccessLog_InvolvesSensitiveData_CreateDate` | InvolvesSensitiveData, CreateDate | 篩選／監控敏感訪問事件 |
| `IX_SysAccessLog_CreateUser_CreateDate` | CreateUser, CreateDate | 追蹤特定使用者的訪問活動（訪問者＋期間） |

原設計（2026-09-08）以 `AccessedBy`／`AccessedAt` 組成並自訂命名：`IX_SysAccessLog_Entity_Time`（EntityName, PrimaryKey, AccessedAt DESC）、`IX_SysAccessLog_Sensitive_Time`（InvolvesSensitiveData, AccessedAt DESC）、`IX_SysAccessLog_User_Time`（AccessedBy, AccessedAt DESC）。

#### 資料表說明與設定

- 資料表說明（`HasComment`）：資料訪問紀錄（append-only），記錄使用者對敏感資料的讀取／匯出／下載事件，僅記被訪問的敏感欄位名稱、不記機密值。
- 每個欄位皆有 zh-TW `.HasComment`；五個標準欄位套用共用說明，`CreateDate`／`CreateUser` 覆寫為「訪問發生時間」「訪問者識別」（FR-003）。
- 原設計 `HasQueryFilter(a => true)`（不納入軟刪除過濾）已於 2026-09-30 取消，改與一般表相同套用軟刪除全域過濾器。

#### 驗證規則與不變條件

| 規則 | 內容 | 對應需求 |
|------|------|---------|
| append-only | 紀錄一經插入不更新、不刪除；`Removed` 恆為 0 | FR-004 |
| 只記欄位名 | `SensitiveFieldsAccessed` 為欄位名 JSON 陣列，僅 `InvolvesSensitiveData=true` 時填入 | FR-005、FR-009 |
| 敏感導向 | Read 僅在實體有 `@SensitiveData` 欄位時記錄；Export／Download 一律記錄 | FR-008 |
| 一次一筆 | 匯出／下載一次操作一筆紀錄，以 `RecordCount` 表示筆數 | Edge Cases |
| IP 可空 | 無法取得來源 IP 時為 NULL，事件仍記錄 | Edge Cases、FR-009 |
| 不再稽核 | `AccessLog` 標記 `[NotAudited]`，寫入不產生 `SysAuditLog` | — |

無狀態機。

#### 與 SPEC-006 資料表異動紀錄的區別

| 面向 | `SysAuditLog`（SPEC-006） | `SysAccessLog`（SPEC-010） |
|------|---|---|
| **紀錄對象** | 資料異動（新增/修改/刪除） | 資料訪問（讀取/匯出/下載） |
| **OldValues/NewValues** | 記錄改前/改後整列快照 | 無快照；僅記錄敏感欄位名稱清單 |
| **敏感值遮罩** | 快照中遮罩為 `"***"` | 不記錄敏感值，改記欄位名稱 |
| **寫入一致性** | 與資料變更同交易（強一致） | 旁路記錄（盡力寫入） |
| **寫入失敗** | 導致資料變更回滾 | 可觀測告警，主流程不中斷 |
| **主鍵** | `int` Vogen ID（ValueGenerator 產生） | `bigint IDENTITY`（DB 產生） |
| **軟刪除** | 有 `Removed` 欄 + 全域過濾（恆為 0） | 有 `Removed` 欄 + 全域過濾（恆為 0；2026-09-30 修訂前無） |
| **五欄填入** | `AuditLogInterceptor` 經建構子填入（在 `AuditableEntityInterceptor` 之後才加入） | `AuditableEntityInterceptor` 於新增時填入 |

### API 契約

#### 記錄服務介面（現行）

`IAccessLogRecorder`（`TcmClinic.Api.Core.Auditing`）：

- `Task RecordAsync<TEntity>(AccessLogRequest request, CancellationToken cancellationToken = default)`
- `Task RecordAsync(Type entityType, AccessLogRequest request, CancellationToken cancellationToken = default)`
- `AccessLogRequest(AccessType AccessType, string EntityName, string? PrimaryKey, int RecordCount, string? Purpose = null)`：單筆讀取填主鍵字串、匯出多筆時傳 `null`；「是否涉及敏感資料／敏感欄位清單」由記錄服務依實體型別的 `@SensitiveData` 自動判定，呼叫端不需指定。

代表性示範掛載點 `GET /users`（SPEC-004，授權 `MenuAccess:accountMaintenance`）回應不變；回應前呼叫 `RecordAsync<User>(new AccessLogRequest(AccessType.Read, nameof(User), PrimaryKey: null, 本頁筆數, "使用者清單查詢"))`。

#### 原 contracts/access-log-api.md

> ⚠️ **2026-10-02 已移除**：本查詢 API 與其選單已刪除（見 Clarifications Session 2026-10-02），以下內容僅保留作歷史紀錄；紀錄的寫入不受影響。
>
> 實作路由為 `GET /access-logs`、授權原則 `MenuAccess:/admin/access-logs`（[Source: Code] `git show c8e5872^:backend/src/TcmClinic.Api.Web/Endpoints/Auditing/GetAccessLogsEndpoint.cs`），與下方契約原文的 `/auditing/access-logs` 不同。

**版本**: 1.0 | **日期**: 2026-09-08

##### 端點定義

###### GET /auditing/access-logs

**功能**: 查詢資料訪問紀錄，支援多維度篩選與分頁。

**授權**: 需具「資料訪問紀錄」選單項可見性（沿用 SPEC-005 選單導向授權）

**查詢參數**

| 參數名 | 型別 | 必填 | 說明 |
|--------|------|------|------|
| `accessedBy` | string | N | 訪問者（帳號）；支援 wildcard 模糊查詢（可選） |
| `startDate` | datetime(ISO 8601) | N | 查詢起始時間（含）；格式 `2026-09-08T00:00:00Z` |
| `endDate` | datetime(ISO 8601) | N | 查詢結束時間（含）；格式 `2026-09-08T23:59:59Z` |
| `entityName` | string | N | 被訪問的實體名稱（如 `SysUser`）；精確匹配 |
| `primaryKey` | string | N | 被訪問資料的主鍵；精確匹配 |
| `involvesSensitiveData` | boolean | N | 篩選：true 僅敏感訪問，false 僅非敏感訪問，null 不篩選 |
| `page` | integer(≥1) | N | 分頁編號（預設 1） |
| `pageSize` | integer(1-100) | N | 每頁筆數（未指定、0 或負數時取系統參數「查詢列表分頁筆數」（初始 10；系統參數維護 FR-008(b)），最大 100；2026-10-02 前預設 20） |

**請求示例**

```http
GET /auditing/access-logs?accessedBy=audituser&involvesSensitiveData=true&page=1&pageSize=20
Authorization: Bearer <token>
```

**回應 200 OK**（JSON 物件，分頁結果）

```json
{
  "items": [
    {
      "id": 1001,
      "accessType": "Read",
      "entityName": "SysUser",
      "primaryKey": "123",
      "recordCount": 1,
      "accessedBy": "audituser",
      "clientIp": "192.168.1.100",
      "involvesSensitiveData": true,
      "sensitiveFieldsAccessed": ["PasswordHash", "PasswordSalt"],
      "purpose": "User profile audit",
      "accessedAt": "2026-09-08T10:30:45Z"
    },
    {
      "id": 1000,
      "accessType": "Export",
      "entityName": "SysUser",
      "primaryKey": null,
      "recordCount": 450,
      "accessedBy": "admin",
      "clientIp": "10.0.0.5",
      "involvesSensitiveData": true,
      "sensitiveFieldsAccessed": ["PasswordHash"],
      "purpose": "Monthly user audit export",
      "accessedAt": "2026-09-08T09:15:00Z"
    }
  ],
  "totalCount": 1042,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 53
}
```

**回應欄位說明**

| 欄位 | 型別 | 說明 |
|------|------|------|
| `id` | long | 紀錄 ID（bigint） |
| `accessType` | string | 訪問類型（"Read", "Export", "Download"） |
| `entityName` | string | 被訪問的實體名稱 |
| `primaryKey` | string \| null | 被訪問的主鍵；匯出多筆時為 null |
| `recordCount` | integer | 涉及筆數 |
| `accessedBy` | string | 訪問者帳號 |
| `clientIp` | string \| null | 來源 IP；背景作業為 null |
| `involvesSensitiveData` | boolean | 是否涉及敏感資料 |
| `sensitiveFieldsAccessed` | array[string] \| null | 敏感欄位名稱清單 |
| `purpose` | string \| null | 訪問目的說明 |
| `accessedAt` | datetime(UTC) | 訪問時間戳 |

> 2026-09-30 修訂（設計決策 R7）：資料表欄位改為五個標準欄位，本 API 對外名稱不變——`accessedBy`（參數與回應）對應 `CreateUser`、`accessedAt` 對應 `CreateDate`；`startDate`／`endDate` 篩選與排序改比對 `CreateDate`。

**Link Header（GitHub 風格分頁）**

```http
Link: <http://api/auditing/access-logs?page=1&pageSize=20>; rel="first",
      <http://api/auditing/access-logs?page=2&pageSize=20>; rel="next",
      <http://api/auditing/access-logs?page=53&pageSize=20>; rel="last"
```

**錯誤回應**

| 狀態碼 | 場景 | 回應本文要點 |
|--------|------|-------------|
| 400 Bad Request | 查詢參數驗證失敗（如 pageSize > 100） | ProblemDetails：`title`「One or more validation errors occurred.」、`errors.pageSize`「The field pageSize must be between 1 and 100.」、`traceId` |
| 401 Unauthorized | 未認證（無有效 token） | ProblemDetails：`title`「Unauthorized」、`status` 401 |
| 403 Forbidden | 已認證但無「資料訪問紀錄」選單權限 | ProblemDetails：`title`「Forbidden」、`detail`「User does not have access to view access logs.」 |
| 500 Internal Server Error | 伺服器錯誤（如日誌表查詢失敗） | ProblemDetails：`title`「An error occurred while processing your request.」、`traceId` |

##### 效能預算

- **目標**: ≤ 300ms p95（Principle XIV）
- **負載假設**: 最多查詢 1M 筆紀錄，50M 筆總行數
- **優化策略**:
  - 三個複合索引已規劃（見本節「資料模型 › 索引」）
  - 查詢未指定每頁筆數時取系統參數「查詢列表分頁筆數」（初始 10），最大 100 筆
  - 無 JOIN；AccessLog 為自洽表

##### 向後相容性

- **版本 1.0**: 初始發行，無先前版本。
- **未來擴充**（不在本規格範圍）：若需另新增「敏感欄位值頻率統計」等複雜分析，應在新的 `/auditing/access-log-analytics` 端點實作，保持本查詢 API 簡潔。

### 設計決策（原 research）

> 決策編號 R1～R7 沿用原研究文件，供程式碼註解追溯（如 `research R7`）。部分決策的範例程式與現行實作不同，以「實作註記」標示，現行行為以程式為準。

#### R1 敏感欄位檢測機制
- **決定**：直接複用 SPEC-006 既有的 `@SensitiveDataAttribute` 機制。
- **理由**：`SensitiveDataAttribute` 已在 `TcmClinic.Api.SharedKernel` 完整實作，支援屬性級別標記與繼承，設計高度通用；`AuditSnapshotBuilder.IsSensitive()` 提供檢測邏輯，可改編供 AccessLog 使用；避免重複實作，降低維護成本。
- **替代方案**：
  - 新增 `@AccessLogSensitiveData` 屬性：重複定義，維護困難。否決。
  - 硬編碼敏感欄位清單：不可擴充，新增欄位需修改記錄邏輯。否決。
- **在 AccessLog 中的應用**：(1) 判定 `InvolvesSensitiveData`：遍歷被訪問實體欄位，任一含 `SensitiveDataAttribute` 即為 `true`；(2) 記錄 `SensitiveFieldsAccessed`：蒐集所有敏感欄位名稱，序列化為 JSON 陣列（如 `["PasswordHash", "PasswordSalt"]`）；(3) 確保機密不入庫：記錄的是敏感欄位**名稱清單**，永不記錄機密值（對比 AuditLog 會遮罩值為 `"***"`）。
- **標記方式**：在實體屬性上加 `[SensitiveData]`（如 `User.PasswordHash`、`User.PasswordSalt`）；一般個資（如 `Account`）不標記，照實記錄。

#### R2 代表性示範掛載點
- **決定**：選定使用者讀取端點作為敏感訪問示範（原決策寫 `/users/{id}`）。
- **理由**：`SysUser` 表含密碼雜湊（`PasswordHash` + `PasswordSalt`，已標記 `@SensitiveData`）；端點回傳的 DTO 已排除敏感欄位（安全），但端點內部讀取時敏感欄位物理存在；適合示範「查詢 API 層面無機密外洩，但訪問日誌精確記錄敏感欄位被讀取過」；非匯出／下載，更能驗證「敏感導向」的一般查詢機制。
- **替代方案**：
  - 新建專用示範端點：額外工作，無實質價值。否決。
  - 匯出端點示範：目前無匯出實作，需等新功能。否決。
  - 列表查詢示範：通常無單一資料敏感欄位，示範效果有限（保留觀察）。
- **實作策略**：在 `GetUsersEndpoint` 或 `GetUserByIdEndpoint` 內呼叫記錄服務，傳入實體名、主鍵、`AccessType.Read` 與敏感欄位清單；不修改端點簽名或回應結構。
- **實作註記**：專案無 GetById 端點，實際掛載於既有清單端點 `GET /users`（`GetUsersEndpoint`），`PrimaryKey` 為 `null`、`RecordCount`＝本頁筆數（原任務 T008 註記；[Source: Code] `backend/src/TcmClinic.Api.Web/Endpoints/Users/GetUsersEndpoint.cs:54-59`）。

#### R3 訪問事件擷取點
- **決定**：Endpoint 層顯式呼叫（推薦）；Query Handler 層共用邏輯（Mediator Pipeline Behavior）留作未來擴充。
- **理由**：讀取操作沒有 SaveChanges 可攔截。三個候選位置比較如下：

  | 位置 | 侵入度 | 可控性 | 複用性 | 推薦 |
  |------|--------|--------|--------|------|
  | Endpoint 層 | 低 | 高 | 中 | ✅ 推薦 |
  | Query Handler 層 | 中 | 低（CQRS 單一責任） | 高 | 未來擴充用 |
  | Specification | 高 | 低 | 低 | 不建議 |

  Endpoint 層的優點：訪問日誌是「端點級」的關切，不屬於領域邏輯；可條件式記錄（例如僅對特定角色記錄、偽生產環境跳過）；不需改動既有 Query Handler，降低副作用。
- **替代方案**：
  - Query Handler 層：日後若多個 Query 都需記錄，可建立 `AccessLogPipelineBehavior`，以標記介面 `IAccessLoggedQuery` 自動記錄；需要時另開 feature 實作。
  - Specification 層：侵入度高、可控性與複用性低。否決。

#### R4 來源 IP 擷取策略
- **決定**：方案 A——擴充 `ICurrentUser.SourceIp`（可為 `null`）。`SystemCurrentUser` 回傳 `null`（背景作業無 IP）；Web 層 `HttpContextCurrentUser` 由 HttpContext 取得。原決策範例為優先取 `X-Forwarded-For` 第一段，否則取 `Connection.RemoteIpAddress`。
- **理由**：`ICurrentUser` 已是「當前操作上下文」抽象，IP 是其自然屬性；Infrastructure 層不直接耦合 HttpContext，由適配層封裝（符合 Clean Architecture）；無 HttpContext 時自動為 `null`；`IHttpContextAccessor` 已註冊，DI 無需改變。
- **替代方案**：方案 B，新增 `IHttpContextAccessor` 直接注入記錄服務——Infrastructure 層耦合 HttpContext（違反分層），且每個需要 IP 的服務都要重複注入。可行但不採用。
- **實作註記**：`ICurrentUser.SourceIp` 以預設介面成員回傳 `null`；`HttpContextCurrentUser.SourceIp` 只取 `Connection.RemoteIpAddress`，**未**解析 `X-Forwarded-For`（[Source: Code] `backend/src/TcmClinic.Api.Web/Services/HttpContextCurrentUser.cs`）。

#### R5 敏感欄位清單序列化
- **決定**：使用 JSON 陣列格式（`["FieldName1", "FieldName2"]`）。
- **理由**：標準格式，容易給 API 用戶端解析與前端展示；可用 JSON 函式做欄位篩選查詢（如查存取過 `PasswordHash` 的紀錄）；與 `AuditLog.NewValues`／`OldValues` 的 JSON 格式一致，降低序列化複雜度。
- **替代方案**：
  - 逗號分隔字串：查詢困難（substring matching 脆弱）。否決。
  - EF Core computed column：不必要，欄位清單於記錄時一次生成。否決。

#### R6 日誌寫入失敗策略（盡力寫入）
- **決定**：在記錄服務內以 async try-catch 包覆，失敗時記錄可觀測告警，不拋出，主流程不中斷。
- **理由**：FR-014 要求日誌寫入失敗不阻擋使用者查詢／匯出；與 SPEC-006 不同，`006` 異動紀錄為「同交易強一致」（寫入失敗即資料變更回滾），AccessLog 為「旁路」（寫入失敗只留告警）；即使日誌表滿或 DB 臨時故障，使用者仍能繼續工作。
- **替代方案**：
  - Background Queue：部分場景不適用、複雜度高，留作未來優化。
  - Silent failure（無告警）：違反 Principle XVII，問題無法觀測。否決。
- **可觀測性機制（原設計）**：結構化日誌以 `[AccessLogFailure]` 關鍵字記錄；計數 `access_log_write_failures_total`；15 分鐘內失敗數超過閾值即告警。
- **實作註記**：實際監控關鍵字為 `[ACCESS_LOG]`，以 `LogError` 記錄 EntityName／AccessType／AccessedBy，並把未落庫的實體從 ChangeTracker 卸除；程式中未見 `access_log_write_failures_total` 度量（[Source: Code] `backend/src/TcmClinic.Api.Infrastructure/Data/Auditing/AccessLogRecorder.cs:62-75`）。

#### 相關設計決策：記錄觸發點與敏感欄位清單蒐集時機
- **讀取（查詢）**：僅在涉及敏感欄位時記錄（避免日誌爆量），判定方式為掃描 Projection／DTO，若含敏感欄位則記錄。
- **匯出／下載**：不論是否敏感一律記錄；這類操作本身少見，適合無差別稽核。
- **敏感欄位清單蒐集時機**：原建議先在端點層依 Spec／Domain 知識硬編碼（控制精確、不依賴執行時反射，但需手動維護），日後若常新增敏感欄位再改為執行時反射（記錄時掃描 Entity 型別的 `@SensitiveData`，並建「Entity 型別 → 敏感欄位清單」快取）。
- **實作註記**：實作直接採執行時反射，由 `SensitiveFieldDetector` 依實體型別判定（列層級：型別有任一 `@SensitiveData` 即視為涉及敏感資料），呼叫端不需指定欄位清單（[Source: Code] `backend/src/TcmClinic.Api.Core/Auditing/SensitiveFieldDetector.cs`）。

#### R7 五個標準欄位（2026-09-30 修訂；DATABASE-SCHEMA §1、§6、§8；Clarifications Session 2026-09-30）
- **決定**：
  - `AccessedBy`／`AccessedAt` 改為 `CreateUser`／`CreateDate`，新增 `ModifyDate`／`ModifyUser`（與 Create* 同值）與 `Removed`（恆為 0）；`AccessLog` 實作 `IAuditable`、`ISoftDelete`，套用軟刪除全域過濾器。僅 `bigint IDENTITY` 主鍵維持例外。
  - 五欄由既有 `AuditableEntityInterceptor` 填入：`AccessLogRecorder` 將紀錄加入 `DbContext` 後才 `SaveChanges`，新增的 `AccessLog` 會經過該攔截器；建構子不再收訪問者與時間（同 SPEC-013 `SysUserLog` 做法）。`[ACCESS_LOG]` 失敗告警仍帶 `currentUser.UserName`。
  - Migration 以「改名」保留既有紀錄，`Modify*` 以 `Create*` 回填；三個索引改為 EF 預設命名。
  - 查詢 API 對外的 `accessedBy`（參數與回應）、`accessedAt`（回應）名稱不變，由 `CreateUser`／`CreateDate` 投影；排序與期間篩選改比對 `CreateDate`。
- **理由**：所有表格式統一（使用者 2026-09-30 裁決）；做法與 SPEC-006 `SysAuditLog`、SPEC-013 `SysUserLog` 一致。API 名稱是業務語意，與資料庫欄位名分開，維持不變可避免前端改版。
- **替代方案**：
  - 由 `AccessLogRecorder` 自行填五欄（同 SPEC-006）：SPEC-006 是因攔截器順序不得不如此；本表會經過 `AuditableEntityInterceptor`，交給它填可避免兩處重複邏輯。否決。
  - API 一併改名為 `createUser`／`createDate`：屬破壞性變更，且欄位名對查詢者不直觀；無必要。否決。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)：無違規。記錄服務放在 Infrastructure 層、介面定義於 Core、原查詢 API 在 Web 層（Principle I）；`AccessType` 以 SmartEnum 表示（II）；寫入為 Command、查詢為 Query（III）；訪問者身分取自 `ICurrentUser`，敏感判定在基礎設施層（IV）；以單元、整合與護欄測試驗收（V、XV）；所有欄位 `.HasComment` 為繁體中文（VIII）；機密只記欄位名、IP 可空不 fail hard、寫入失敗有可觀測告警（XVII）；無硬編碼機密（XVIII）。

偏離與例外：
- **Principle VII（強型別 ID）**：主鍵採 `bigint IDENTITY`、不用 Vogen，屬 append-only 稽核表例外（DATABASE-SCHEMA §6）。
- **Principle XI（標準欄位）**：原設計免除 `Removed`／`ModifyDate`／`ModifyUser`，只有 `AccessedAt`／`AccessedBy`；2026-09-30 修訂後此例外取消，五個標準欄位照常具備（設計決策 R7），只保留主鍵例外。
- **Principle XIV（效能預算）**：查詢 API ≤300ms p95 未做專屬壓測（原任務 T020），查詢 API 移除後作廢；日誌寫入為旁路，不得使主流程逾越既有預算。

## Test Plan（測試計畫）

### 自動化測試

- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/Auditing/SensitiveFieldDetectorTests.cs`：AC-1、AC-3、AC-4（敏感欄位偵測與機密護欄，3 個測試）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AccessLogSchemaTests.cs`：AC-1、AC-10、AC-11（綱要、說明、索引與五欄填入，2 個測試）。
- [x] 迴歸測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbMenuTreeSeedTests.cs`、`backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs`：AC-14（`/admin/access-logs` 不再種入、不出現在 `/me/menus`）。
- 已移除（`c8e5872`）：`TcmClinic.Api.FunctionalTests/Auditing/GetAccessLogsQueryTests.cs`（掛載點記錄、紀錄無機密值、分頁 Link 標頭）、`TcmClinic.Api.FunctionalTests/Auditing/GetAccessLogsAuthorizationTests.cs`（200／403／401）。

執行：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.UnitTests -- -class "TcmClinic.Api.UnitTests.Auditing.SensitiveFieldDetectorTests"
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests -- -class "TcmClinic.Api.IntegrationTests.Auditing.AccessLogSchemaTests"
```

預期：全綠。

### 手動走查

前置：本機已安裝 Docker；資料庫已套用 migration；`User` 的 `PasswordHash`、`PasswordSalt` 已標記 `@SensitiveData`。

1. 於 Development 啟動後端，以具「帳號維護」（`accountMaintenance`）可見性的帳號（如開發模式 `admin`）呼叫 `POST /auth/login` 登入，取得 Cookie。
2. 呼叫代表性示範掛載點 `GET /users?page=1&pageSize=20`：回應 200，DTO 不含 `PasswordHash`。
3. 直接查詢資料庫 `SysAccessLog` 最新一筆：`AccessType`＝`Read`、`EntityName`＝`User`、`PrimaryKey` 為 NULL、`RecordCount`＝本頁筆數、`InvolvesSensitiveData`＝1、`SensitiveFieldsAccessed`＝`["PasswordHash","PasswordSalt"]`、`Purpose`＝「使用者清單查詢」、`CreateUser`＝登入帳號、`ModifyUser`／`ModifyDate` 與 Create* 同值、`Removed`＝0、`ClientIp` 有值。
4. 確認該筆（及整張表）任何欄位都不含實際密碼、雜湊值、salt、token 或連線字串（SC-002，出現率 0%）。
5. 非敏感查詢不記錄：原驗收情境以「訂單清單」為例，但專案目前只有 `GET /users` 一個掛載點，其他端點不會呼叫記錄服務；判定邏輯由單元測試 `Plain_type_has_no_sensitive_fields` 與 Code Review（AC-3）涵蓋。
6. 確認 `/me/menus` 不再出現 `/admin/access-logs`，且查詢 API `GET /access-logs` 已不提供（2026-10-02 移除）。
7. （停止適用，僅供歷史參考）原驗收情境 3、5：無「資料訪問紀錄」權限者查詢回 403、具權限者回 200；`pageSize` 超過 100 回 400；篩選後只回該使用者的敏感訪問紀錄；回應帶 GitHub 風格 Link 標頭。

## Dependencies（相依）

- SPEC-006 資料表異動紀錄：`@SensitiveData` 宣告式標記、append-only 稽核表例外、`[NotAudited]`。
- SPEC-005 選單導向授權模型：原查詢 API 的授權（已移除）；示範掛載點 `GET /users` 的選單授權。
- SPEC-004 使用者管理與登入：`GET /users` 端點與 `User` 實體（含 `PasswordHash`／`PasswordSalt`）。
- 既有 `ICurrentUser`／`AuditableEntityInterceptor`：訪問者與五個標準欄位的來源。
- docs/DATABASE-SCHEMA.md §1、§6、§8：標準欄位與 append-only 例外規範。

## Risks（風險）

| 取捨／限制 | 說明 | 影響 |
|-----|------|------|
| 永久保留 | 日誌表無自動清理，由維運決定清理策略 | DB 磁碟持續成長；已知、可接受的成本 |
| 旁路記錄 | 日誌寫入失敗不阻擋主流程，僅產生告警 | 與 SPEC-006 異動紀錄的「同交易強一致」策略刻意不同；隱私稽核可能漏記，但不影響使用者操作 |
| 敏感導向訪問日誌 | 只記錄敏感訪問或一律記錄的匯出/下載，不記錄所有讀取 | 避免日誌爆量；但全量讀取稽核需另行實作 |
| 代表性示範 | 只以一個端點示範，不 retrofit 所有現有讀取端點 | 快速交付可重用能力；既有端點逐一接入屬未來工作 |

已知限制：
- 代表性示範掛載點只有 `GET /users`；其他敏感讀取／匯出端點待後續逐一接入。匯出／下載路徑目前沒有實際呼叫端（T009 延後）。
- 背景作業記錄時 `ClientIp` 為 NULL（無 HttpContext）。
- 日誌寫入失敗時主流程不中斷但只留告警，監控需觀看日誌系統；原設計的失敗度量與告警規則未實作。
- 來源 IP 未解析 `X-Forwarded-For`，部署在反向代理後時可能記到代理位址（設計決策 R4 實作註記）。
- 查詢 API 移除後，端到端（掛載點→紀錄）的自動化驗證只剩記錄服務層級測試，`GET /users` 是否確實寫入目前靠手動走查。

## 實作紀錄（Archive）

- **狀態**：已實作（2026-09-08）；2026-09-30 資料表規範對齊收斂完成；2026-10-02 查詢 API 與選單移除。
- **主要提交**：
  - `b44f61b` Phase 0 研究＋Phase 1 設計 → `25d493f` 任務清單 → `897c60c` 需求品質審查清單通過（45/45）。
  - `a8fca17` 實作資料訪問日誌（新增 9 個測試；全套 54 單元 + 55 功能 = 109 passed）。
  - `9e27d6b` 規格對齊資料表規範 → `f1a1525` 收斂任務 T025–T028 → `b58d612` 先寫失敗測試（T025）→ `38f094d` 五個標準欄位（T026–T027）→ `7e19602` 完成（全套 179 單元 + 83 整合 + 204 功能綠，T028）。
  - `c8e5872` 移除舊後台選單與訪問紀錄查詢 API（全套 257 單元 + 126 整合 + 236 功能綠）。
- **審查紀錄**：
  - 規格品質檢查（原 `checklists/requirements.md`）：16/16 通過（2026-09-08，規格釐清後）。
  - 需求品質檢查（原 `checklists/data-access-logging.md`）：CHK001～CHK045 全數通過（2026-09-08）。審查時拍板的決定：「涉及敏感資料」以**列層級**判定，該列任一欄位標記敏感即記錄（CHK008、CHK043）；「一般查詢」原指單筆讀取（GetById），清單查詢視為異常情況（CHK042，實作後改以清單端點 `GET /users` 示範）；代表性掛載點修改既有端點、不另建（CHK044）；旁路寫入沒有併發或競態問題（CHK028）；日誌表本身不加密、不再稽核，資料庫層級保護已足夠（CHK037）。
- **測試數**：實作時新增 9 個（`SensitiveFieldDetectorTests` ×3、`GetAccessLogsQueryTests` ×3、`GetAccessLogsAuthorizationTests` ×3）；2026-09-30 新增 `AccessLogSchemaTests` ×2；2026-10-02 查詢 API 相關 6 個隨 API 刪除，目前剩 5 個。
- **原 Spec Kit 產出**：計畫、研究、資料模型、驗收指南、任務清單、API 契約、checklists 已併入本檔後移除；原始檔可由 `git show c8e5872:"specs/其他/資料訪問日誌/tasks.md"` 等方式追溯。

### 延後與作廢事項

#### T009 匯出事件記錄示範（延後）

出口：specs/README.md 延後事項 DEF-002

專案目前沒有匯出／下載端點可以掛載。`IAccessLogRecorder` 已支援 `AccessType.Export`／`Download`，能力已備妥；FR-016 只要求「至少一個」代表性掛載點，已由 `GET /users` 滿足。等第一個匯出／下載端點出現時再接入。

- **read_first**：
  - `backend/src/TcmClinic.Api.Core/Auditing/IAccessLogRecorder.cs`：記錄介面與 `AccessLogRequest` 簽名。
  - `backend/src/TcmClinic.Api.Infrastructure/Data/Auditing/AccessLogRecorder.cs`：Export／Download 一律記錄、Read 才做敏感判定的分支。
  - `backend/src/TcmClinic.Api.Web/Endpoints/Users/GetUsersEndpoint.cs`：既有掛載寫法（第 54～59 行）。
  - `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/AccessLogSchemaTests.cs`：記錄服務整合測試寫法。
  - 本規格 FR-008、FR-016、SC-001、AC-2。
- **action**：
  1. modify：第一個匯出／下載端點（檔案路徑【待確認】，目前專案沒有此類端點）注入 `IAccessLogRecorder`，產出結果後呼叫 `RecordAsync<TEntity>(new AccessLogRequest(AccessType.Export 或 AccessType.Download, nameof(TEntity), PrimaryKey: null, 匯出筆數, 目的), ct)`。
  2. add：在 `backend/tests/TcmClinic.Api.IntegrationTests/Auditing/` 新增整合測試，斷言匯出紀錄的 `AccessType`、`RecordCount`、`InvolvesSensitiveData`。
- **acceptance_criteria**：
  - AC-1：**GIVEN** 含 `@SensitiveData` 欄位的實體 **WHEN** 使用者匯出 N 筆 **THEN** `SysAccessLog` 新增一筆 `AccessType=Export`、`RecordCount=N`、`InvolvesSensitiveData=true` 且只含欄位名的紀錄。
  - AC-2：**GIVEN** 不含敏感欄位的實體 **WHEN** 匯出或下載 **THEN** 仍新增一筆紀錄（`InvolvesSensitiveData=false`、`SensitiveFieldsAccessed` 為 NULL）。
  - AC-3（迴歸）：**GIVEN** 訪問日誌寫入失敗 **WHEN** 使用者匯出 **THEN** 匯出仍成功，並留下 `[ACCESS_LOG]` 錯誤告警。
- **verification**：
  - `grep -rnE "AccessType\.(Export|Download)" backend/src/TcmClinic.Api.Web/Endpoints` → 至少一處。
  - `dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests -- -class "TcmClinic.Api.IntegrationTests.Auditing.<新測試類別>"` → 全綠。

#### T020 查詢 API ≤300ms p95 效能測試（已作廢）

已作廢。查詢 API（US2，`GET /access-logs`）已於 2026-10-02 移除，見本檔 Clarifications Session 2026-10-02 及 specs/README.md 本規格列「⚠️ 2026-10-02 查詢 API（US2）與『資料訪問紀錄』選單移除，寫入不變」。原延後說明：查詢已建 3 個複合索引，功能測試實測回應約 20ms，正式 p95 壓測（1,000 筆插入 × 100 次查詢）原定等有專屬效能測試專案時補上。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：2026-10-01 資料表規範對齊修訂的紅燈 commit：`d45a863`（T025）。初版實作沒有單獨提交紅燈 commit，無紀錄可補。
