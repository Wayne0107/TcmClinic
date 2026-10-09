# [SPEC-012] Feature: 角色權限維護 API 對齊 CHB.Web 前端契約

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-012 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 管理業務 › 系統管理作業 › 角色權限維護（`rolePermission`） |
| 建立日期 | 2026-09-21 |
| 最後更新 | 2026-10-04（由 Spec Kit 格式轉為 UDS SDD 格式；內容未變） |
| 原分支 | `012-role-permission-api` |
| 相關規格 | 沿用 SPEC-002 後台選單與角色權限管理（角色模型）、SPEC-005 選單導向授權模型（並修改其角色維護端點守門鍵為 `rolePermission`）、SPEC-007 CHB選單與角色種子／SPEC-008 選單API欄位對齊前端、SPEC-004 使用者管理與登入（使用者↔角色）；被 SPEC-016 系統參數維護部分修改（預設每頁筆數） |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

> **狀態沿革**：Implemented（2026-09-30）；2026-10-02 移除舊後台選單（見 Clarifications Session 2026-10-02）。
>
> ⚠️ **部分被 SPEC-016 系統參數維護修改**：角色維護清單預設每頁筆數改取系統參數「查詢列表分頁筆數」（其 FR-008(b)，初始值 10，值不變）。⚠️ 2026-10-06 SPEC-016 移除該參數，預設改為固定 10（值不變，[ADR-007](../../../../docs/adr/ADR-007-default-page-size-fixed.md)）；API 契約內文於 SPEC-016 實作時一併更新。
>
> ⚠️ **2026-10-02 舊後台選單移除**：`PUT /roles/{id}` 改由 `rolePermission` 授權、`GET /roles` 改為 `accountMaintenance` 或 `rolePermission` 任一可見即可（見 Clarifications Session 2026-10-02、FR-014）。
>
> ⚠️ **部分被 SPEC-064 對外識別碼 UUIDv7 修改（2026-10-08）**：角色 API 的識別碼（路徑 `{id}`、回應 `id`、可見性請求 `grantedMenuItemIds`）改為 UUIDv7 字串，不再公開整數主鍵（[SPEC-064](../../../其他/對外識別碼UUIDv7/spec.md)）。
>
> 2026-09-30 釐清：新增角色僅限 Custom；防鎖死依 SPEC-005 選單導向授權模型 FR-005 維持不做（曾暫定之 FR-018 防鎖死已撤回）。

## Overview（概述）

CHB.Web「管理業務 → 系統管理作業 → 角色權限維護」頁原接前端 mock 資料；後端雖已有角色模組，但回傳資料與畫面所需不一致。本規格讓後端提供此頁所需的完整資料與行為：分頁角色清單（含角色種類與是否已指派）、新增角色（後端自動產碼、僅限一般使用者類）、設定角色可見選單（必填版本權杖防並行覆蓋）、刪除未指派角色（伺服器端強制保護）。範圍僅限後端，作為前端（另一團隊維護）串接的契約來源真相。

原始需求（使用者描述）：「配合 CHB.Web『角色權限維護』頁面，檢查後端角色相關 API 需要改什麼、走規格流程；前端配合後端契約，補齊後端未提供、但前端畫面需要的資料。」

## Motivation（動機）

CHB.Web 的「管理業務 → 系統管理作業 → 角色權限維護」頁面目前接的是前端 mock 資料。後端其實已具備一套角色模組（清單、新增、更新、刪除、以及「角色↔可見選單」設定），但**回傳的資料與前端畫面所需不一致**，導致此頁無法以真實後端運作。

本規格的範圍是**後端**：讓後端提供「角色權限維護」頁運作所需的完整資料與行為，作為前端（另一團隊維護）串接的契約來源真相。此工作與已完成的 SPEC-008（選單 API 欄位對齊前端）性質相同，沿用 SPEC-002／SPEC-005 的角色與授權模型、SPEC-005 的選單授權機制。

- **US1（P1）**：這是此頁的核心。沒有正確的清單資料（尤其「角色種類」與「是否已指派」），畫面無法正確呈現，也無法判斷哪些角色能刪除，其餘操作都失去前提。
- **US2（P2）**：建立新角色是維護作業的基本能力，但排在檢視之後——沒有可用清單就無從管理。
- **US3（P2）**：「設定權限」是此頁的關鍵價值（決定角色能看到／操作哪些功能），但需先有角色清單為前提，故與新增同級、排在檢視之後。
- **US4（P3）**：清理用途，頻率低且風險需控管，故優先度最低。

## Clarifications（釐清紀錄）

### Session 2026-09-21

- Q: 角色維護端點的伺服器端授權該以哪個選單項為準？ → A: 對齊現行前端 canonical 值 `rolePermission`（管理業務‧系統管理作業）；本功能種子須補上 value 為 `rolePermission` 的選單項並授予系統管理員（SM）角色。不沿用只對舊 frontend 有效的 `/admin/roles`。
- Q: 「設定權限」儲存是否必須攜帶版本權杖（rowVersion）？ → A: 必填；缺版本權杖→400、版本不符→409。後端獨立保證並行不靜默覆蓋（SC-004）。
- Q: 維護頁清單是否保留關鍵字搜尋？ → A: 不保留；清單僅支援分頁，無搜尋／篩選。
- Q: FR-013「系統內建角色不可刪」與 005「超管可刪除」衝突，SUPERADMIN 如何處理？ → A: 本功能只用 SM，不特別處理 SUPERADMIN。SM 種為 `RoleType=System`（受保護不可刪）；SUPERADMIN 種為 `RoleType=Custom`（不特別處理），依 005 維持可刪除、保留其既有測試。刪除保護判定＝單純 `RoleType=System`，無 SUPERADMIN 例外碼。

### Session 2026-09-29

- Q: 刪除角色時，要不要支援選填的並行版本權杖（rowVersion），也就是有帶且版本不符就回 409？ → A: 不支援。刪除不做並行版本檢查；並行控制（FR-015）僅涵蓋「設定權限」。刪除已有伺服器端強制保護（System 類／已指派不可刪）且為軟刪除。

### Session 2026-09-30

- Q: 要不要防止系統管理員角色（如 SM）把自己對「角色權限維護」（rolePermission）的存取權拿掉？ → A: 不防。維持 [SPEC-005 選單導向授權模型](../../../其他/選單導向授權模型/spec.md)（005）FR-005「不實作任何防鎖死」之運維取捨：即使取消勾選後全系統無任何角色保有 `rolePermission`，「設定權限」仍允許；若發生鎖死，由具資料庫存取權者直接修改資料表恢復。（曾暫定「全系統至少一角色保有 `rolePermission`」，因與 005 衝突，於同日收斂前撤回。）
- Q: 新增角色時，要不要允許把角色種類選成「系統管理員類（System）」？ → A: 不允許。新增角色只能建立 `Custom`（一般使用者類）；請求送 `roleType=System` → 拒絕（400）、不建立。`System` 類僅來自種子（如 SM）。`roleType` 欄位仍為必填（維持既有契約形狀）。
- Q: 判斷角色「是否已指派」時，停用中的使用者算不算數？ → A: 算。只要存在任一未刪除的使用者↔角色指派（不論該使用者啟用或停用）即為已指派、不可刪除。

### Session 2026-10-02

背景：CHB 選單已併入規格（見 SPEC-007 CHB選單與角色種子），範本早期的舊後台選單（`/admin/users`、`/admin/roles`、`/admin/audit-logs`、`/admin/access-logs`）不再使用。使用者決定直接移除並修訂相關規格，不另開規格。

- Q: 原本仍留在 `/admin/roles` 的 `GET /roles`、`PUT /roles/{id}` 如何授權？ → A: `/admin/roles` 移除；`PUT /roles/{id}` 改由 `rolePermission` 授權（納入 FR-014）；跨頁共用查詢 `GET /roles` 改為 `accountMaintenance`（帳號維護）或 `rolePermission` 任一可見即可。

## Requirements（需求）

### Requirement: US1 檢視角色清單（含種類與指派狀態）（P1）

The system SHALL 提供分頁的角色清單，每列含角色代碼、名稱、說明、角色種類與是否已指派。

系統管理人員進入「角色權限維護」頁，看到一份分頁的角色清單。每一列顯示：角色代碼、角色名稱、角色說明、角色種類；並依「該角色是否已被指派給任何使用者」與「是否為系統內建角色」決定各列可用的操作（設定權限、刪除）。獨立驗證方式：以一組已知角色資料呼叫清單查詢，驗證回傳含分頁資訊（本頁資料＋總筆數）與每列的角色代碼／名稱／說明／種類／是否已指派。涵蓋 FR-001、FR-002、FR-003、FR-004。

#### Scenario: AC-1 分頁回傳總筆數
- **GIVEN** 系統中有多於一頁的角色
- **WHEN** 查詢第 1 頁（每頁 10 筆）
- **THEN** 回傳該頁角色與可計算總頁數的總筆數

#### Scenario: AC-2 已指派角色標示
- **GIVEN** 某角色已被至少一名使用者指派
- **WHEN** 查詢清單
- **THEN** 該列標示為「已指派」

#### Scenario: AC-3 未指派角色標示
- **GIVEN** 某角色未被任何使用者指派
- **WHEN** 查詢清單
- **THEN** 該列標示為「未指派」

#### Scenario: AC-4 角色種類標示
- **GIVEN** 系統內建管理角色（種子，如 SM）
- **WHEN** 查詢清單
- **THEN** 該列的角色種類標示為「系統管理員」類，其餘角色（含所有經新增建立者）標示為「一般使用者」類

### Requirement: US2 新增角色（P2）

The system SHALL 允許以角色名稱、角色種類（僅一般使用者類）與選填說明新增角色，並由後端自動產生不重複的角色代碼。

系統管理人員填寫角色名稱、角色種類（僅可為一般使用者類）與（選填）說明後新增一個角色；系統建立成功並回到最新清單。獨立驗證方式：送出一筆合法的新增請求，驗證角色被建立、可於清單查得、且角色代碼依規則產生且不重複。涵蓋 FR-004、FR-005、FR-006、FR-007。

#### Scenario: AC-5 合法新增
- **GIVEN** 合法的角色名稱與種類
- **WHEN** 送出新增
- **THEN** 系統建立角色並可於清單查得

#### Scenario: AC-6 代碼重複拒絕
- **GIVEN** 會造成角色代碼重複的輸入
- **WHEN** 送出新增
- **THEN** 系統拒絕並回報可理解的錯誤訊息（不建立資料）

#### Scenario: AC-7 缺少必填拒絕
- **GIVEN** 缺少必填（角色名稱或種類）
- **WHEN** 送出新增
- **THEN** 系統拒絕並指出缺少的欄位

#### Scenario: AC-8 不可新增 System 類
- **GIVEN** 角色種類為「系統管理員類（System）」
- **WHEN** 送出新增
- **THEN** 系統拒絕並指出新增角色只能為一般使用者類（不建立資料）

### Requirement: US3 設定角色可見選單（設定權限）（P2）

The system SHALL 提供指定角色的可授權選單樹與目前授權狀態，並允許以一組可授權葉節點覆寫該角色的可見選單。

系統管理人員對某一角色開啟「設定權限」，看到可授權的選單樹並勾選；其中已授權的項目預先勾起。儲存後，該角色的可見選單被更新為勾選結果。獨立驗證方式：對指定角色讀取其可授權選單樹與目前授權狀態，變更勾選後儲存，再次讀取應反映新結果。涵蓋 FR-008、FR-009、FR-010、FR-015。

#### Scenario: AC-9 讀取可授權選單樹與現況
- **GIVEN** 指定一個角色
- **WHEN** 開啟設定權限
- **THEN** 系統回傳可授權的選單樹並標示每個可授權項目目前是否已授權

#### Scenario: AC-10 覆寫可見選單
- **GIVEN** 勾選了一組可授權項目
- **WHEN** 儲存
- **THEN** 該角色的可見選單被覆寫為該組項目

#### Scenario: AC-11 含不可授權節點拒絕
- **GIVEN** 勾選內容包含不可授權的節點（如僅為分類的容器節點）
- **WHEN** 儲存
- **THEN** 系統拒絕並回報原因（不部分寫入）

### Requirement: US4 刪除未被指派的角色（P3）

The system SHALL 允許刪除未被任何使用者指派的角色，並於伺服器端拒絕刪除已指派角色與系統內建角色。

系統管理人員刪除一個「未被任何使用者指派」的角色；已被指派的角色不提供刪除。獨立驗證方式：對未指派角色執行刪除後於清單查不到；對已指派角色執行刪除被拒。涵蓋 FR-011、FR-012、FR-013。

#### Scenario: AC-12 刪除未指派角色
- **GIVEN** 一個未被指派的角色
- **WHEN** 刪除
- **THEN** 該角色不再出現於清單

#### Scenario: AC-13 已指派角色不可刪
- **GIVEN** 一個已被指派的角色
- **WHEN** 嘗試刪除
- **THEN** 系統拒絕並回報「角色已被指派無法刪除」

#### Scenario: AC-14 內建角色不可刪
- **GIVEN** 系統內建管理角色
- **WHEN** 嘗試刪除
- **THEN** 系統拒絕（不可刪除內建角色）

### 功能需求條文（Functional Requirements）

- **FR-001**: 系統 MUST 提供角色清單查詢，支援分頁（頁碼、每頁筆數）並回傳可計算總頁數的總筆數。
- **FR-002**: 角色清單每列 MUST 包含：角色代碼、角色名稱、角色說明、角色種類、是否已指派。
- **FR-003**: 系統 MUST 判定並回傳每個角色的「是否已指派」，定義為「是否存在任何未刪除的使用者↔角色指派」，停用中的使用者亦計入。
- **FR-004**: 系統 MUST 於角色資料上提供可獨立設定的「角色種類」欄位，用以區分系統管理員類（System）與一般使用者類（Custom）；新增角色時 MUST 攜帶種類且僅接受 `Custom`，送 `System` MUST 拒絕（400、不建立）。`System` 類僅由種子建立（如 SM）。（決策：採「新增可設定的欄位」，需資料表異動與 Migration。）
- **FR-005**: 系統 MUST 允許以角色名稱、角色種類（僅 `Custom`，見 FR-004）與選填說明新增角色。
- **FR-006**: 系統 MUST 為每個角色維持唯一的角色代碼，並在重複時拒絕新增。
- **FR-007**: 系統 MUST 以既有「角色代碼（Code）」欄位作為角色唯一識別碼。既有內建角色的代碼由種子資料**固定寫死且不變更**；由於前端新增表單不提供代碼，系統 MUST 於新增角色時**自動產生不重複的代碼**。（決策：沿用 Code 欄位；種子固定；新角色由後端產生代碼。）
- **FR-008**: 系統 MUST 提供指定角色的「可授權選單樹」，並標示每個可授權項目目前是否已授權。
- **FR-009**: 系統 MUST 允許以一組可授權項目覆寫指定角色的可見選單；空清單代表清除全部可見選單。
- **FR-010**: 系統 MUST 拒絕將不可授權節點（如僅供分類的容器節點）或不存在的項目納入設定，且不得部分寫入。
- **FR-011**: 系統 MUST 允許刪除「未被任何使用者指派」的角色，刪除後該角色不再出現於清單。
- **FR-012**: 系統 MUST 於**伺服器端強制**拒絕刪除「已被指派」的角色，並回報可理解的原因（不得僅依賴前端隱藏按鈕）。（決策：後端強制。）
- **FR-013**: 系統 MUST 拒絕刪除「系統管理員類（RoleType=System）」角色（例如 SM）。判定為單純 `RoleType=System`。SUPERADMIN 種為 `RoleType=Custom`，維持 005 之可刪除行為，不在此保護範圍。
- **FR-014**: 所有角色維護操作（查詢、新增、設定權限、刪除）MUST 在伺服器端受「角色權限維護」選單（value `rolePermission`）之授權控管；未授權者一律拒絕。（2026-10-02：原留在 `/admin/roles` 的 `PUT /roles/{id}` 一併改由 `rolePermission` 控管；跨頁共用 `GET /roles` 為 `accountMaintenance` 或 `rolePermission`；見 Clarifications Session 2026-10-02。）
- **FR-015**: 系統 MUST 對同一角色的並行修改提供防護，避免一方變更被另一方靜默覆蓋。「設定權限」儲存請求 MUST 攜帶版本權杖（rowVersion）並由伺服器端比對：缺版本權杖→拒絕（400）、版本不符（他人已先變更）→拒絕（409），不得覆寫。並行版本檢查僅適用「設定權限」；刪除角色不做版本檢查、不接受 rowVersion（刪除已受 FR-012/013 伺服器端保護且為軟刪除）。
- **FR-016**: 對外契約（欄位語意、錯誤形狀、狀態語意）MUST 與系統既有 API 慣例一致，並以 CHB.Web「角色權限維護」頁所需資料為對齊目標。
- **FR-017**: 系統 MUST 於選單種子提供 value 為 `rolePermission` 的「角色權限維護」選單項，並授予系統管理員（SM）角色可見，使 CHB 系統管理員具備存取本功能之權限（配合 FR-014 的授權判定）。

### 邊界情境（Edge Cases）

- 查詢頁碼超出總頁數時，回傳空頁但仍提供正確總筆數（前端可回退至有效頁）。
- 對不存在的角色設定權限或刪除時，回報「找不到角色」。
- 設定權限送出空清單時，代表「清除該角色所有可見選單」（合法操作），而非錯誤。
- 管理者把自身角色的 `rolePermission` 取消勾選：允許（管理者自身隨即失去存取權，屬預期結果）；即使因此全系統無任何角色保有 `rolePermission` 亦不阻擋，依 SPEC-005 FR-005 由人工改資料表恢復。
- 兩人同時「設定權限」同一角色時，需避免其中一方的變更被靜默覆蓋（並行安全，FR-015）；刪除不做並行版本檢查。
- 無「角色權限維護」對應選單授權的使用者呼叫任一操作時，一律拒絕（伺服器端強制）。

### 關鍵實體（Key Entities）

- **角色（Role）**：可被指派給使用者的權限集合。關鍵屬性：角色代碼（唯一，Code；內建角色種子固定、新角色由系統產生）、角色名稱、角色說明、**角色種類（獨立欄位；新增時僅能為 Custom，System 僅來自種子）**、是否為系統內建（＝System 類）。
- **使用者↔角色指派（User-Role Assignment）**：使用者與角色的關聯；用以判定角色「是否已指派」。
- **角色↔可見選單（Role-Menu Visibility）**：角色可見（＝可操作）的選單項目集合；「設定權限」即維護此關聯。
- **選單項目（Menu Item）**：選單樹節點，區分「容器（分類，不可授權）」與「可授權葉節點」。

### 假設（Assumptions）

- CHB.Web 為另一團隊維護的正式前端；本規格只交付**後端**契約與行為，前端依此契約自行調整（沿用 SPEC-007／SPEC-008 的分工）。
- 「是否已指派」以「存在任一未刪除的使用者↔角色指派」為準，停用中的使用者亦計入（釐清 2026-09-30 定案）。
- 「設定權限」沿用既有「角色↔可見選單」機制與其可授權／容器節點規則（SPEC-005／SPEC-008），本規格不重新定義選單授權模型，僅確保前端可讀取樹狀＋目前授權狀態並覆寫。
- 回應資料形狀以「與既有 API 慣例一致」為準；前端 mock 中的欄位命名（如 data/total/success 包裝）僅為 mock 慣例，非契約，前端 composable 依實際回傳對齊。
- 系統內建管理角色沿用既有種子與命名（SPEC-005：不再具授權特權，但仍為不可刪除之內建角色）。
- 角色種類為新增之欄位（需 Migration）；既有種子角色須給定合理的預設種類值。透過 API 新增的角色一律為 `Custom`；本功能不提供修改角色種類的能力。
- 新增角色的代碼由後端自動產生且不重複；產生規則（如流水號格式）為實作細節，於技術設計定案（見 Technical Design › 設計決策 R5）。
- 清單僅提供分頁，不含搜尋／篩選（目前無此需求；日後如需，另補 FR）。
- 「設定權限」的並行控制以版本權杖（rowVersion）為必填契約；前端串接時須於載入後回帶該版本權杖。

## Acceptance Criteria（驗收條件）

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、FR-001 | 第 1 頁筆數＝pageSize，回傳可計算總頁數的總筆數 | `backend/tests/TcmClinic.Api.FunctionalTests/RoleMaintenanceEndpointsTests.cs` › `Maintenance_list_pages_by_page_size` |
| AC-2 | US1 情境 2、FR-002、FR-003、SC-002 | 已指派角色 `isAssigned=true`，清單欄位與實際資料 100% 相符 | `RoleMaintenanceEndpointsTests` › `Maintenance_list_includes_type_assigned_and_paging` |
| AC-3 | US1 情境 3、FR-003 | 未指派角色 `isAssigned=false` | `RoleMaintenanceEndpointsTests` › `Unassigned_role_is_marked_not_assigned` |
| AC-4 | US1 情境 4、FR-004 | 種子 SM＝System，其餘（含 SUPERADMIN 與新建角色）＝Custom | `RoleMaintenanceEndpointsTests` › `Maintenance_list_includes_type_assigned_and_paging`；`backend/tests/TcmClinic.Api.IntegrationTests/Data/RoleTypeSeedTests.cs` › `Seeds_role_types_correctly`、`New_role_defaults_to_custom` |
| AC-5 | US2 情境 1、FR-005、FR-007 | 合法新增回 201 並可於清單查得；未帶代碼時自動產生 `R###` | `RoleMaintenanceEndpointsTests` › `Create_without_code_autogenerates_r_code`；`backend/tests/TcmClinic.Api.FunctionalTests/RoleCrudEndpointsTests.cs` › `Create_role_then_appears_in_list` |
| AC-6 | US2 情境 2、FR-006 | 代碼重複回 400、不建立 | `RoleCrudEndpointsTests` › `Duplicate_code_returns_400` |
| AC-7 | US2 情境 3、FR-005 | 缺必填或超長回 400 並指出欄位 | `RoleMaintenanceEndpointsTests` › `Create_with_missing_or_invalid_fields_returns_400`、`Create_with_invalid_role_type_returns_400` |
| AC-8 | US2 情境 4、FR-004 | 送 `roleType=System` 回 400、不建立 | `RoleMaintenanceEndpointsTests` › `Create_with_system_role_type_returns_400_and_does_not_create` |
| AC-9 | US3 情境 1、FR-008 | 回傳選單樹與各葉 `granted`，角色 rowVersion 於 `ETag` 標頭 | `backend/tests/TcmClinic.Api.FunctionalTests/RoleVisibilityEndpointsTests.cs` › `Grant_leaf_is_reflected_in_visibility` |
| AC-10 | US3 情境 2、FR-009 | 儲存後可見選單被覆寫；空清單清除全部授權 | `RoleVisibilityEndpointsTests` › `Grant_leaf_is_reflected_in_visibility`、`Set_empty_visibility_clears_all_grants` |
| AC-11 | US3 情境 3、FR-010 | 含容器節點回 400、不部分寫入 | `RoleVisibilityEndpointsTests` › `Granting_a_container_returns_400` |
| AC-12 | US4 情境 1、FR-011 | 刪除未指派 Custom 角色回 204，清單不再出現 | `RoleCrudEndpointsTests` › `Delete_unassigned_custom_role_succeeds` |
| AC-13 | US4 情境 2、FR-012、SC-003 | 已指派角色刪除回 400（任何管道 0 例外通過） | `RoleCrudEndpointsTests` › `Delete_assigned_superadmin_returns_400` |
| AC-14 | US4 情境 3、FR-013 | System 類角色（SM）刪除回 400 | `RoleCrudEndpointsTests` › `Delete_system_role_returns_400`；`backend/tests/TcmClinic.Api.UnitTests/RoleAggregate/RoleTypeTests.cs` › `Role_system_is_system_role` |
| AC-15 | SC-004、FR-015 | 設定權限缺 rowVersion 回 400、過期 rowVersion 回 409，不靜默覆蓋 | `RoleVisibilityEndpointsTests` › `Set_visibility_without_rowversion_returns_400`、`Set_visibility_with_stale_rowversion_returns_409` |
| AC-16 | SC-005、FR-014 | 無 `rolePermission` 者對所有角色維護操作（含 `PUT /roles/{id}`）成功率 0%（403）；僅具 `rolePermission` 者可操作 | `RoleMaintenanceEndpointsTests` › `Maintenance_ops_without_role_permission_return_403`、`Maintenance_list_without_permission_returns_403`、`Create_with_role_permission_only_succeeds` |
| AC-17 | FR-017 | 種子提供 `rolePermission` 選單並授予 SM；重跑種子冪等 | `RoleTypeSeedTests` › `Sm_is_granted_role_permission_menu`、`Reseeding_keeps_sm_system_idempotent` |
| AC-18 | FR-007、設計決策 R5 | 自動產碼取現存 `R###` 最大序號 +1，不回收已軟刪除代碼 | `backend/tests/TcmClinic.Api.UnitTests/RoleAggregate/RoleCodeGeneratorTests.cs` › `Takes_max_plus_one`；`RoleMaintenanceEndpointsTests` › `Autogenerated_code_is_not_reused_after_soft_delete` |
| AC-19 | Edge Case 1 | 頁碼超出總頁數回空清單但 totalCount 正確 | `RoleMaintenanceEndpointsTests` › `Maintenance_list_out_of_range_page_returns_empty_items` |
| AC-20 | Edge Case 2 | 對不存在角色設定權限或刪除回 404 | `RoleVisibilityEndpointsTests` › `Get_visibility_for_missing_role_returns_404`；手動：Test Plan 手動走查第 8 步（刪除不存在角色） |
| AC-21 | Edge Case 4 | 取消所有角色的 `rolePermission` 不被防鎖死攔截 | `backend/tests/TcmClinic.Api.FunctionalTests/MenuAuthorizationTests.cs` › `Clearing_visibility_is_allowed_no_antilockout` |
| AC-22 | SC-001、FR-016 | 不使用前端 mock 即可完成檢視、新增、設定權限、刪除；錯誤形狀與既有慣例一致（409 為通用訊息） | 手動：Test Plan 手動走查第 1–7 步；手動：Code Review 確認 `backend/src/TcmClinic.Api.Web/Program.cs` 並行衝突 409 訊息為通用文字（原任務 T033） |

> AC-13 以 SUPERADMIN（已指派給種子 admin）驗證「已指派不可刪」，此行為同時修改了 SPEC-005 US2「超級管理員可刪除」的結果（屬一般角色共通規則，非超管特例）。
>
> AC-16 自 2026-10-02 起涵蓋 `PUT /roles/{id}`（原守門鍵 `/admin/roles` 移除）。

## Technical Design（技術設計）

### 技術脈絡

- 語言／框架：C# / .NET 9（原技術設計記載）。
- 相依：FastEndpoints（REPR）、Mediator、Ardalis.Result、Ardalis.Specification、Ardalis.SmartEnum、Vogen（強型別 ID／值物件）、EF Core。
- 儲存：SQL Server（設計時工廠、執行期 Aspire、測試 Testcontainers 皆為 SQL Server，無 SQLite）；表 `SysRoles`、`SysUserRoles`、`SysRoleTasks`（角色↔可見選單，實體 `RoleMenuVisibility`）、`SysTasks`（選單，實體 `MenuItem`）。（表名後續由 SPEC-002／SPEC-004 改為單數。）
- 測試：xUnit；TcmClinic.Api.UnitTests（領域／授權）、TcmClinic.Api.IntegrationTests（EF/Seed/Config）、TcmClinic.Api.FunctionalTests（端點契約）。
- 專案類型：web-service（後端；前端 CHB.Web 由另一團隊依此契約串接）。
- 效能：後台管理規模（角色數十筆、使用者數百筆以內）；清單查詢與 `isAssigned` 計算須避免 N+1——固定兩次查詢（全部角色、全部使用者含指派），於記憶體分頁與彙總。若使用者規模顯著成長，改為只投影 `RoleId` 的彙總查詢。
- 約束：不得破壞既有 `GET /roles`（角色挑選器與 `RoleCrudEndpointsTests` 依賴其陣列形狀）；授權於伺服器端強制；文件與註解 zh-TW。

技術取徑：沿用 Clean Architecture 既有分層與慣例，以「新增可設定欄位 + 新增維護專用查詢端點 + 強化既有命令」為主，最小化對既有消費者的破壞：

1. **清單**補「角色種類」「是否已指派」與分頁（不動既有 `GET /roles`，另開一個維護頁專用端點，避免破壞角色挑選器與既有測試）。
2. **新增**支援「角色種類」（僅接受 Custom；System 僅來自種子，釐清 2026-09-30），且因前端不帶代碼 → 後端**自動產生**不重複代碼（保留：呼叫端若明確提供代碼仍沿用，向後相容）。
3. **設定權限**沿用既有 `menu-visibility` 端點；補樂觀並行控制避免靜默覆蓋（FR-015）。
4. **刪除**於伺服器端**強制**「已指派不可刪」與「內建角色不可刪」（FR-012/013）。

### 受影響元件

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.Core/RoleAggregate/Role.cs` | 新增 `RoleType` 屬性 + `UpdateRoleType` + `IsSystemRole`；`RowVersion` 並行權杖 |
| `backend/src/TcmClinic.Api.Core/RoleAggregate/RoleType.cs` | 新增：SmartEnum {System, Custom} |
| `backend/src/TcmClinic.Api.Core/RoleAggregate/Specifications/RoleSpecs.cs` | 新增 `AllRolesIncludingDeletedSpec`（產碼跨軟刪除掃描）；清單沿用既有 `RolesOrderedSpec` |
| `backend/src/TcmClinic.Api.UseCases/Roles/GetRolesForMaintenance.cs` | 新增：維護頁查詢（分頁 + 種類 + isAssigned）與 `RoleMaintenanceItemDto`、`RoleMaintenanceListResult` |
| `backend/src/TcmClinic.Api.UseCases/Roles/CreateRole.cs` | 修改：RoleType 僅接受 Custom；代碼可省略→自動產生 |
| `backend/src/TcmClinic.Api.UseCases/Roles/RoleCodeGenerator.cs` | 新增：自動產生 `R###` 代碼（取現存最大序號 +1） |
| `backend/src/TcmClinic.Api.UseCases/Roles/DeleteRole.cs` | 修改：已指派／System 角色 → 拒絕 |
| `backend/src/TcmClinic.Api.UseCases/Roles/GetRoleMenuVisibility.cs` | 修改：結果附角色 rowVersion；節點移除 `IsContainer` |
| `backend/src/TcmClinic.Api.UseCases/Roles/SetRoleMenuVisibility.cs` | 修改：rowVersion 必填並行權杖檢查（缺→400、不符→409） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Config/RoleConfig.cs` | 修改：`RoleType` 欄位映射 + `RowVersion` 並行權杖 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20260921074209_AddRoleTypeAndRowVersionToRole.cs` | 新增 Migration |
| `backend/src/TcmClinic.Api.Infrastructure/Data/ChbMenuSeedData.cs` | 修改：SM=System；種入 `rolePermission` 選單並授予 SM（FR-017） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/SeedData.cs` | 不變（SUPERADMIN 維持 Custom，不特別處理；釐清 2026-09-21） |
| `backend/src/TcmClinic.Api.Web/Endpoints/Roles/GetRolesForMaintenanceEndpoint.cs` | 新增：`GET /roles/maintenance` |
| `backend/src/TcmClinic.Api.Web/Endpoints/Roles/CreateRoleEndpoint.cs` | 修改：請求加 RoleType（僅 Custom）；Code 選填；請求驗證 |
| `backend/src/TcmClinic.Api.Web/Endpoints/Roles/GetRoleVisibilityEndpoint.cs` | 修改：以 `ETag` 回應標頭輸出 rowVersion |
| `backend/src/TcmClinic.Api.Web/Endpoints/Roles/SetRoleVisibilityEndpoint.cs` | 修改：請求加 rowVersion（必填） |
| `backend/src/TcmClinic.Api.Web/Endpoints/Roles/DeleteRoleEndpoint.cs` | 行為改變由 UseCase 承擔；端點回應碼補 400 |
| `backend/src/TcmClinic.Api.Web/Endpoints/Roles/UpdateRoleEndpoint.cs` | 2026-10-02：授權改 `MenuAccess:rolePermission` |
| `backend/src/TcmClinic.Api.Web/Program.cs` | 409 並行衝突訊息改為通用文字 |

以上角色維護端點授權皆改為 `MenuAccess:rolePermission`。

### 資料模型

沿用既有資料表；本功能新增 `SysRoles.RoleType` 欄位與 `SysRoles.RowVersion` 並行權杖，其餘為讀取彙總。

#### Role（`SysRoles`）— 修改

既有欄位：`Id`（RoleId, Vogen）、`Code`（RoleCode, ≤50, 未刪除唯一）、`Name`（RoleName, ≤20）、`Description`（≤200, 可空）、稽核欄位、`Removed`（軟刪除）。

| 新增欄位 | 型別 | 約束 | 說明 |
|----------|------|------|------|
| `RoleType` | SmartEnum `RoleType`（存字串）| `NVARCHAR(20)` NOT NULL，預設 `Custom` | 角色種類：`System` / `Custom`。Migration 對既有列回填 `Custom`，種子再更新系統類。 |
| `RowVersion` | `byte[]`（`rowversion`）| 並行權杖 | 樂觀並行控制（比照 `LcApplication`）。 |

**領域行為（Role）**：
- 建構子新增 `RoleType`（預設 `Custom`）。
- `UpdateRoleType(RoleType)`：變更種類（供未來編輯；本功能新增時設定）。
- `IsSystemRole` 衍生：`RoleType == RoleType.System`（用於刪除保護）。SM=System 受保護；SUPERADMIN 種為 Custom 故可刪（維持 SPEC-005）。

**驗證規則**：
- `Code` 未刪除列唯一（既有篩選唯一索引）。
- 新增時 `RoleType` 必填且僅接受 `Custom`；`System` → 拒絕（400）。`System` 僅由種子建立（如 SM），本功能不提供修改種類（釐清 2026-09-30）。
- `Name` 1–20 字、`Code` 1–50 字（既有 Vogen 驗證）。

#### RoleType（新值物件）— 新增

SmartEnum，比照 `LcApplicationStatus`：

| 值 | Name | 前端對應 | 語意 |
|----|------|----------|------|
| `System` | `"System"` | 預設-(系統管理員) | 內建管理角色；不可刪除、通常隱藏設定權限 |
| `Custom` | `"Custom"` | 自訂-(一般使用者) | 一般角色；可設定權限、未指派時可刪除 |

#### UserRole（`SysUserRoles`）— 不變（讀取用）

用於計算 `IsAssigned`：存在任一未刪除 `UserRole.RoleId == role.Id` 即為已指派。既有唯一索引 `(UserId, RoleId)` filter `[Removed]=0`。

#### RoleMenuVisibility（`SysRoleTasks`）— 不變

「設定權限」沿用既有覆寫語意（[SetRoleMenuVisibility.cs](../../../../backend/src/TcmClinic.Api.UseCases/Roles/SetRoleMenuVisibility.cs)）：僅接受具路由的非容器葉節點，空清單代表清除。

#### 讀取模型（DTO）

**RoleMaintenanceItemDto（新增，維護頁清單列）**

```
Id: int
Code: string          // 角色代碼（roleNo）
Name: string          // 角色名稱
Description: string?   // 角色說明
RoleType: string      // "System" | "Custom"
IsAssigned: bool      // 是否已被任何使用者指派
RowVersion: string    // base64 並行權杖（供後續變更帶回）
```

**RoleMaintenanceListResult（新增，分頁包裝，比照 SPEC-011）**

```
Items: RoleMaintenanceItemDto[]
Page: int
PageSize: int
TotalCount: int
```

**既有 DTO**

- `RoleDto(Id, Code, Name, Description)`：`GET /roles`（角色挑選器）維持不變。
- `RoleMenuVisibilityNodeDto(Id, Name, Route, Granted, Children)`：`GET /roles/{id}/menu-visibility`。回應 body 維持節點陣列；角色 `rowVersion`（base64）以 `ETag` 回應標頭傳遞（含引號），供 `PUT` 必填回帶。**移除 `IsContainer` 欄位**：CHB.Web 以「`Children`（子節點）有無」判斷容器/葉節點，不使用此欄位（詳 SPEC-007 §容器判定）；且 CHB 容器 `Route` 非 null，該欄位在 CHB 情境恆為 `false`、不可靠。容器/葉之區分由消費端以 `Children` 是否為空判定。

#### 關聯圖（文字）

```
Role (SysRoles) 1 ──── * UserRole (SysUserRoles)      → 決定 IsAssigned
Role (SysRoles) 1 ──── * RoleMenuVisibility           → 「設定權限」覆寫
MenuItem 1 ──── * RoleMenuVisibility                  → 可授權葉節點（非容器）
```

#### Migration 計畫

1. `AddRoleTypeAndRowVersionToRole`：
   - `SysRoles` 新增 `RoleType NVARCHAR(20) NOT NULL DEFAULT 'Custom'`（回填既有列為 `Custom`）。
   - `SysRoles` 新增 `RowVersion rowversion`。
   - 加欄位註解（zh-TW，比照既有 `HasComment`）。
2. 種子更新（於 `ChbMenuSeedData` 冪等執行）：`SM` → `System`。`SUPERADMIN` 與其餘角色維持 `Custom`（由 Migration 預設回填，無需特別處理）。

> 本專案一律使用 SQL Server（設計時工廠、執行期 Aspire、測試 Testcontainers 皆為 SQL Server；無 SQLite path），`rowversion` 原生支援；並行權杖比照既有 `LcApplication`（`IsRowVersion()`），不另創機制。

無狀態機。

### API 契約

#### 原 contracts/roles-maintenance.md（端點契約：角色權限維護）

> 以下保留原檔 §1–§5 編號與節標題；設計決策 R7 所稱「contracts §4」即本節 §4。

風格沿用專案既有慣例（FastEndpoints REPR、`Result` → HTTP/ProblemDetails、路由前綴 `/`）。授權皆為 `MenuAccess:rolePermission`（伺服器端強制，FR-014；釐清 2026-09-21 定案，取代原 `/admin/roles`）。需配套種子提供 `rolePermission` 選單項並授予 SM（FR-017）。

對應前端消費點：[useRolePermission.ts](../../../../CHB.Web/src/composables/useRolePermission.ts)、[RolePermissionSetting.vue](../../../../CHB.Web/src/components/rolePermission/RolePermissionSetting.vue)。前端 mock 的 `POST /rolePermission/*` 為 mock 慣例，非契約；前端 composable 依本契約調整（沿用 SPEC-007／SPEC-008 分工）。

##### 1. 角色清單（維護頁）— 新增

`GET /roles/maintenance?page={page}&pageSize={pageSize}`

- **用途**：US1 檢視角色清單（含種類、是否已指派、分頁）。
- **查詢參數**：`page`（預設 1）、`pageSize`（未指定、0 或負數時為 10（[ADR-007](../../../../docs/adr/ADR-007-default-page-size-fixed.md)；2026-10-02～10-06 取系統參數「查詢列表分頁筆數」），最大 100）。（無搜尋／篩選，釐清 2026-09-21 定案。）
- **200 回應**：
  ```json
  {
    "items": [
      {
        "id": 3,
        "code": "R003",
        "name": "經辦",
        "description": "經辦",
        "roleType": "Custom",
        "isAssigned": true,
        "rowVersion": "AAAAAAAAB9E="
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalCount": 8
  }
  ```
  另附 GitHub 風格 `Link` 標頭（first/prev/next/last），比照 `GET /lc-applications`。
- **403**：無 `rolePermission`（角色權限維護）選單權限。
- **驗收對應**：FR-001/002/003/004、SC-001/002。

##### 2. 新增角色 — 修改既有 `POST /roles`

`POST /roles`

- **用途**：US2 新增角色。
- **請求（修改）**：
  ```json
  { "name": "分行覆核", "roleType": "Custom", "description": "分行覆核", "code": null }
  ```
  - `name`（必填, ≤20）、`roleType`（必填，**僅接受 `Custom`**；`System` 僅由種子建立，釐清 2026-09-30）、`description`（選填, ≤200）。
  - `code`（**選填**）：提供則沿用並檢查唯一（向後相容既有測試）；省略/為空則由後端自動產生 `R###`（FR-006/007）。
- **201 回應**：`{ "id": 9 }`（沿用既有 `CreateRoleResponse`）。
- **400**：`code` 重複、缺必填、欄位超長、`roleType` 非 `Custom`（含送 `System`，不建立）。
- **403**：無權限。
- **驗收對應**：FR-005/006/007。
- **相容性**：既有以 `{code,name,description}` 呼叫者不受影響；新增 `roleType` 對舊呼叫者為必填 → 既有測試需增補 `roleType`。

##### 3. 取得角色可見選單（設定權限-讀）— 沿用既有

`GET /roles/{id}/menu-visibility`

- **回傳**：選單樹＋每葉 `granted`（[GetRoleMenuVisibility.cs](../../../../backend/src/TcmClinic.Api.UseCases/Roles/GetRoleMenuVisibility.cs)）。
- **增補（必要）**：以 `ETag` 回應標頭（值為 base64 rowVersion，含引號）附該角色並行權杖，供 §4 `PUT` 儲存時**必填**回帶（R7）。
- **欄位調整**：節點移除 `IsContainer` 欄位（`Id, Name, Route, Granted, Children`）。CHB.Web 以 `Children` 有無判斷容器/葉節點、不使用此欄位；且 CHB 容器 `Route` 非 null 使該欄位恆為 `false`、不可靠。唯一消費者 CHB.Web 未使用，故移除。容器/葉之區分由消費端以 `Children` 是否為空判定。
- **404**：找不到角色。
- **驗收對應**：FR-008；前端 US3 應改為呼叫此端點載入樹與現況（取代目前用 `useMenu` 的作法）。

##### 4. 設定角色可見選單（設定權限-存）— 沿用/強化既有

`PUT /roles/{id}/menu-visibility`

- **請求**：`{ "id": 3, "grantedMenuItemIds": [12, 13], "rowVersion": "AAAAAAAAB9E=" }`
  - `grantedMenuItemIds`：**整數 MenuItemId** 清單（非前端目前用的字串 `value`；前端需對應）。空清單＝清除全部（合法）。
  - `rowVersion`（**必填**，R7；釐清 2026-09-21 定案）：由 `GET menu-visibility` 載入時取得並回帶，伺服器端比對並行版本。
- **200**：更新成功。
- **400**：含容器或不存在的 id（不部分寫入，既有行為）；或**缺 `rowVersion`**。
  - **容器判定**：以「是否具子節點」判定（對齊 CHB.Web），非以 `Route is null`；CHB 容器 `Route` 非 null，故不可用 Route 判。導覽側（`/me/menus`）仍維持 SPEC-005 之 `Route` 判定（SPEC-007，不改）。
- **404**：找不到角色。
- **409**：`rowVersion` 不符（他人已先變更，並行衝突，R7）。
- **403**：無權限。
- **驗收對應**：FR-009/010/015、SC-004。

##### 5. 刪除角色 — 修改既有 `DELETE /roles/{id}`

`DELETE /roles/{id}`

- **用途**：US4 刪除未指派角色。
- **請求**：路由 `id`。不接受 `rowVersion`、不做並行版本檢查（釐清 2026-09-29 定案；並行控制僅限 §4 設定權限）。
- **204**：刪除成功（軟刪除）。
- **400**：
  - `System` 類（內建）角色 → 拒絕（FR-013）。
  - 已被任一使用者指派 → 拒絕，訊息「角色已被指派無法刪除」（FR-012，**伺服器端強制**）。
- **404**：找不到角色。
- **403**：無權限。
- **驗收對應**：FR-011/012/013、SC-003。

##### 錯誤形狀

沿用既有：`Result.Invalid` → `ValidationProblem`（400）、`Result.NotFound` → 404、並行衝突 → 409 ProblemDetails。訊息 zh-TW。

##### 授權矩陣

| 端點 | Policy |
|------|--------|
| GET /roles/maintenance | MenuAccess:rolePermission |
| POST /roles | MenuAccess:rolePermission |
| GET /roles/{id}/menu-visibility | MenuAccess:rolePermission |
| PUT /roles/{id}/menu-visibility | MenuAccess:rolePermission |
| DELETE /roles/{id} | MenuAccess:rolePermission |

> 註：授權字串 `rolePermission` 為釐清（2026-09-21）定案，對齊 CHB 現行前端選單 value；需配套種子提供該選單項並授予 SM（FR-017 / 設計決策 R8）。既有 Roles 端點（如角色挑選器 `GET /roles`）之授權不在本功能變更範圍。
>
> ⚠️ 2026-10-02：`PUT /roles/{id}` 亦改為 `MenuAccess:rolePermission`；`GET /roles` 改為 `MenuAccess:accountMaintenance|rolePermission`（見 Clarifications Session 2026-10-02、FR-014）。

### 設計決策（原 research）

> 決策編號沿用原研究文件，供程式碼註解追溯。規格層級的三項業務決策（角色種類欄位、代碼沿用/自動產生、刪除後端強制）已於規格建立階段拍板，此處聚焦「如何實作」。

#### R1 角色種類（RoleType）的型別與儲存
- **決定**：以 SmartEnum（`Ardalis.SmartEnum`）定義 `RoleType`，兩個值：`System`（系統管理員類）、`Custom`（一般使用者類）；於 `SysRoles` 以字串欄位 `RoleType` 儲存其 `Name`（`NVARCHAR(20)`, NOT NULL, 預設 `Custom`）。
- **理由**：專案既有 `LcApplicationStatus` 即為 SmartEnum，型別安全且可讀（符合原則 IV/V）；存字串使 DB 可讀、避免魔術數字；預設 `Custom` 讓既有列與新列有明確語意。
- **替代方案**：
  - C# `enum` 存 `int`：較輕，但與既有 SmartEnum 風格不一致、DB 不可讀。
  - Vogen `[ValueObject<string>]` 自由字串：無法限制為兩值，違反有限集合語意。
  - **由代碼衍生（不落地欄位）**：已於規格建立階段被否決（使用者選「新增可設定的欄位」）。
- **前端對應**：前端 `roleType` 選項 `system`／`custom`（[RolePermissionAdd.vue](../../../../CHB.Web/src/components/rolePermission/RolePermissionAdd.vue#L129)）→ 對應 `RoleType.System`／`RoleType.Custom`；清單顯示字串「預設-(系統管理員)」「自訂-(一般使用者)」由前端依 `roleType` 呈現（後端只回 `System`/`Custom` 語意值，符合契約來源真相原則）。

#### R2 種子角色的 RoleType 預設值〔釐清 2026-09-21 修訂：只用 SM〕
- **決定**：僅 CHB 的 `SM`（系統管理員，[ChbMenuSeedData.cs](../../../../backend/src/TcmClinic.Api.Infrastructure/Data/ChbMenuSeedData.cs#L37)）種為 `System`；`SUPERADMIN`（[SeedData.cs](../../../../backend/src/TcmClinic.Api.Infrastructure/Data/SeedData.cs)）與其餘 CHB 角色 `BH/BS/MB/BM` 及所有新建角色皆為 `Custom`。Migration 對既有列回填預設 `Custom`，再由種子將 `SM` 更新為 `System`。
- **理由**：對齊前端「系統管理人員」列隱藏刪除的行為（SM=System 受保護）；SUPERADMIN 不特別處理（維持 SPEC-005 可刪除，避免重新引入超管特例）。
- **替代方案**：SUPERADMIN 也種為 System（否決：會反轉 SPEC-005、破壞其「超管可刪」測試，且使用者指定本規格內不特別處理 SUPERADMIN）。

#### R3 內建/系統角色「不可刪除」的判定來源（FR-013）〔釐清 2026-09-21 修訂：單純 System〕
- **決定**：刪除保護以單純 `RoleType == System` 為判定基準（無代碼例外）。SM=System 受保護；SUPERADMIN 因種為 `Custom`（見 R2）而可刪，自然維持 SPEC-005 行為，無需在刪除邏輯特別處理 SUPERADMIN。
- **理由**：判定最單純、資料驅動；同時滿足「SM 受保護」與「本規格內不特別處理 SUPERADMIN」。
- **替代方案**：`RoleType==System 且 Code≠SUPERADMIN`（否決：需 SUPERADMIN 例外碼；改以 R2 把 SUPERADMIN 種為 Custom 即可免除例外）。

#### R4 是否已指派（isAssigned）的計算（FR-003）
- **決定**：於維護頁查詢中，以 `IReadRepository<User>` + 既有 `UsersOrderedSpec`（含 `UserRoles`）單次讀取全部未刪除使用者，彙總其指派為「已指派角色 id 集合」，再與當頁角色比對填 `IsAssigned`。以既有軟刪除全域過濾器排除已刪除的使用者與指派；停用中的使用者亦計入（FR-003）。刪除保護（FR-012）沿用同一方式判定。
- **理由**：固定查詢次數、避免 N+1（原則 VI）；`UserRole` 非聚合根，不可直接注入其 repository（原任務 T011）；後台規模（角色數十、使用者數百以內）下全量讀取成本可忽略。
- **替代方案**：逐角色 `AnyAsync`（否決：N+1）；讀 `IReadRepository<UserRole>` 以 `GroupBy RoleId` 彙總（否決：`UserRole` 非聚合根）。若使用者規模顯著成長，改為只投影 `RoleId` 的彙總查詢。

#### R5 新增角色的代碼自動產生規則（FR-006/007）
- **決定**：`CreateRoleCommand` 的 `Code` 改為**可選**。呼叫端提供時沿用（向後相容既有 `RoleCrudEndpointsTests`）；未提供時，後端自動產生格式 `R###`（3 碼零填補流水號），取「現存 `R` 開頭且後接數字的代碼」之最大序號 +1（跨未刪除與已刪除列掃描，避免代碼回收造成衝突）。產生後仍以 `RoleByCodeSpec` 做唯一性防護。
- **理由**：符合前端 mock 呈現（R001…）與使用者「沿用 Code 欄位、前端不帶代碼」的指示；種子既有非 `R` 代碼（BH/SM…）不受影響、固定不變。
- **替代方案**：
  - GUID/隨機碼（否決：不可讀、與前端呈現不符）。
  - 由角色名稱推導（規格建立階段已否決）。
  - 前端提供（規格建立階段已否決；且前端表單無此欄位）。
- **並行安全**：產碼與唯一索引（`[Removed]=0` 篩選唯一）併用；極端併發下若撞碼由唯一索引攔截並回報錯誤（可接受，機率極低）。

#### R6 清單分頁與端點形狀（FR-001，且不破壞既有 GET /roles）
- **決定**：**新增**維護頁專用端點 `GET /roles/maintenance`（分頁 + 種類 + isAssigned），回傳物件 `RoleMaintenanceListResult { Items, Page, PageSize, TotalCount }`，比照 SPEC-011 的 `LcApplicationListResult` 與 GitHub 風格 `Link` 標頭。**既有 `GET /roles` 與其 `RoleDto`（陣列）維持不變**，供角色挑選器與既有測試使用。
- **理由**：附加欄位若直接改動 `GET /roles` 的回應形狀（陣列→物件）會破壞既有消費者與 `RoleCrudEndpointsTests`（原則 V 一致性含「不破壞既有契約」）。獨立端點語意清楚、風險最小。
- **替代方案**：
  - 於 `GET /roles` 加可選分頁參數並改回物件（否決：破壞既有陣列形狀）。
  - 僅在既有 DTO 加欄位不分頁（否決：無法滿足 `total` 與分頁）。
- **前端對應**：前端 mock 為 `POST /rolePermission/list`；依 SPEC-007／SPEC-008 分工，前端 composable 改呼叫 `GET /roles/maintenance` 並讀 `Items/TotalCount`（`success` 由 api-service 預設 true，見 [api-service.ts](../../../../CHB.Web/src/api/api-service.ts#L138)）。

#### R7 設定權限的並行控制（FR-015）〔釐清 2026-09-21 定案：必填〕
- **決定**：於 `Role` 加樂觀並行權杖 `RowVersion`（`rowversion`/`byte[]`，比照 SPEC-011 `LcApplication`）。維護頁清單與 `GET /roles/{id}/menu-visibility` 回傳角色的 `rowVersion`；`PUT /roles/{id}/menu-visibility` 的 `rowVersion` 為**必填**（缺→400、不符→409）；設定權限儲存時一併「觸碰」角色（更新 ModifyDate/RowVersion）以偵測併發。`DELETE /roles/{id}` 不做並行版本檢查、不接受 `rowVersion`（釐清 2026-09-29 定案：刪除已有 System／已指派伺服器端保護且為軟刪除）；`PUT /roles/{id}` 不在本功能範圍。
- **理由**：與 SPEC-011 既有並行策略一致（原則 V）；`rowVersion` 必填使**後端可獨立保證** SC-004（並行不靜默覆蓋），不依賴前端是否配合。
- **對接前提**：前端串接時 MUST 於 `GET menu-visibility` 載入後保存 `rowVersion`，並於儲存回帶（詳見 API 契約 §4）。
- **替代方案**：選填漸進式（釐清否決：SC-004 為 MUST，選填無法在預設路徑達成）；無並行控制（否決：違反 FR-015）；資料庫層鎖（否決：過重、與既有樂觀策略不一致）。

#### R8 授權（FR-014）〔釐清 2026-09-21 定案：rolePermission〕
- **決定**：維護頁端點授權以 CHB 現行前端 canonical 選單 value **`rolePermission`** 為準：`Policies("MenuAccess:rolePermission")`。**不沿用** `/admin/roles`（僅對舊 frontend/SUPERADMIN 有效）。
- **配套（FR-017）**：後端選單種子須提供 value 為 `rolePermission` 的「角色權限維護」選單項（對齊前端 [useMenu.ts:166](../../../../CHB.Web/src/composables/useMenu.ts#L166) 的「管理業務‧系統管理作業」結構），並授予 `SM` 角色可見；否則 CHB 系統管理員將被 403 擋下。
- **理由**：選單導向授權（SPEC-005）以「使用者角色是否被授予該選單項」判定；CHB SM 實際能見的是 `rolePermission`，故授權字串須對齊此值。
- **背景漂移**：後端既有種子（2026-09-04 快照）將角色權限維護掛在進口/出口業務下、值為 `importRolePermission`/`exportRolePermission`，與現行前端不一致。本功能僅補入 `rolePermission` 選單項與 SM 授權以自足可用；CHB 整套選單重新對齊屬另案（SPEC-008 型）。
- **替代方案**：`/admin/roles`（否決：CHB SM 無此權）；`importRolePermission`/`exportRolePermission`（否決：語意錯置、與現行前端不符）。

未解決項：無。R7／R8 已於釐清（2026-09-21）定案；關鍵字搜尋已於釐清移除。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../../docs/CONSTITUTION.md)：無違規（初判與設計後複查皆 PASS，無需記錄 Complexity Tracking）。安全性：刪除保護、已指派保護於伺服器端強制，所有操作受 `MenuAccess` 選單授權，無密鑰／個資硬編碼；測試優先：每個 FR 先寫失敗測試；架構一致性：依賴方向不變、Core 零框架相依；類型安全：`RoleType` 以 SmartEnum、代碼沿用 `RoleCode` Vogen VO；系統一致性：分頁與回應形狀比照 SPEC-011、新增維護端點而非破壞既有契約；效能：`isAssigned` 單次讀取彙總避免 N+1；繁體中文：規格、契約、註解、API 描述皆 zh-TW。

## Test Plan（測試計畫）

### 自動化測試

- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/RoleAggregate/RoleTypeTests.cs`：RoleType 兩值、預設 Custom、`IsSystemRole`、`UpdateRoleType`、`FromName`（AC-14）。
- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/RoleAggregate/RoleCodeGeneratorTests.cs`：`R###` 產碼規則（AC-18）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/RoleTypeSeedTests.cs`：種子種類、新建預設 Custom、SM 具 `rolePermission`、冪等（AC-4、AC-17）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/RoleMaintenanceEndpointsTests.cs`：維護清單、新增驗證、自動產碼、授權 403（AC-1～AC-5、AC-7、AC-8、AC-16、AC-18、AC-19）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/RoleCrudEndpointsTests.cs`：新增、代碼重複、刪除保護（AC-5、AC-6、AC-12～AC-14）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/RoleVisibilityEndpointsTests.cs`：設定權限讀寫、容器拒絕、空清單、rowVersion 400／409、404（AC-9～AC-11、AC-15、AC-20）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/MenuAuthorizationTests.cs`：清空可見性不被防鎖死攔截（AC-21）。

執行（需 Docker Desktop）：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.UnitTests
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests -- -class "TcmClinic.Api.IntegrationTests.Data.RoleTypeSeedTests"
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests -- -class "TcmClinic.Api.FunctionalTests.RoleMaintenanceEndpointsTests"
```

預期：新增之單元（RoleType／產碼）、整合（種子／Migration 欄位）、功能（維護端點契約）測試全綠，既有測試不退化。

### 手動走查

前置：資料庫已套用最新 Migration（含 `AddRoleTypeAndRowVersionToRole`）；已種入既有角色（`SUPERADMIN`、CHB `BH/BS/SM/MB/BM`）。（原走查文件記為 SQLite 開發資料庫，本專案一律 SQL Server，見資料模型 Migration 計畫。）

1. **US1 清單**：以具 `rolePermission` 選單權限之帳號呼叫 `GET /roles/maintenance?page=1&pageSize=10`。預期 `items` 含 `code/name/description/roleType/isAssigned/rowVersion` 及 `totalCount`；`SM` 之 `roleType=System`，其餘（含 `SUPERADMIN`）為 `Custom`；被指派過的角色 `isAssigned=true`。呼叫超出總頁數的 `page`：回空 `items` 但 `totalCount` 正確。（FR-001/002/003/004、SC-002）
2. **US2 新增（自動產碼）**：`POST /roles`，body `{ "name": "分行覆核", "roleType": "Custom" }`（不帶 `code`）。預期 201 回 `id`；再查清單可見該角色，`code` 為自動產生的 `R###` 且不重複。
3. **US2 相容與拒絕**：帶 `code` 呼叫仍成功（向後相容）；帶重複 `code` → 400；帶 `"roleType": "System"` → 400（指出 `roleType`），且清單查無該角色。（FR-004/005/006/007）
4. **US3 讀取**：`GET /roles/{id}/menu-visibility` 取得選單樹與各葉 `granted`；角色 `rowVersion` 於 `ETag` 回應標頭。
5. **US3 儲存**：`PUT /roles/{id}/menu-visibility`，body 帶一組**整數 MenuItemId** 與 `rowVersion`。預期 200；再次 `GET` 反映新授權集合。body 含容器節點 id → 400（不部分寫入）。以過期 `rowVersion` 儲存 → 409；缺 `rowVersion` → 400。（FR-008/009/010/015、SC-004）
6. **US4 刪除**：對「未被指派、非 System」角色 `DELETE /roles/{id}` → 204，清單不再出現；對「已被指派」角色 → 400，訊息「角色已被指派無法刪除」；對 `System` 類（`SM`）→ 拒絕。（FR-011/012/013、SC-003）
7. **授權（橫切）**：以**無** `rolePermission` 權限之帳號呼叫上述任一端點 → 403。（FR-014、SC-005）
8. **不存在角色**：對不存在的角色 id 呼叫 `DELETE /roles/{id}` 與 `PUT /roles/{id}/menu-visibility` → 404（Edge Case 2）。

前端對接檢核（另一團隊，非本後端交付範圍）：CHB.Web composable 由 mock 改呼叫：清單→`GET /roles/maintenance`、新增→`POST /roles`、設定權限→`GET/PUT /roles/{id}/menu-visibility`、刪除→`DELETE /roles/{id}`；設定權限需將勾選識別碼由選單 `value`（字串）對應為 `MenuItemId`（整數），並改為載入「該角色」的可見選單樹（帶 roleId）。

完成準則：上述 US1–US4 與授權情境全部通過；SC-001～SC-005 可量測達成；既有 `GET /roles`（角色挑選器）與其測試不受影響。

## Dependencies（相依）

- [SPEC-002 後台選單與角色權限管理](../../../其他/後台選單與角色權限管理/spec.md)：既有角色模型與角色維護能力。
- [SPEC-005 選單導向授權模型](../../../其他/選單導向授權模型/spec.md)：`MenuAccess` 選單授權機制、不做防鎖死（FR-005）；本規格修改其角色維護端點守門鍵為 `rolePermission`。
- SPEC-007 CHB選單與角色種子、[SPEC-008 選單API欄位對齊前端](../../../其他/選單API欄位對齊前端/spec.md)：選單資料、容器判定與前後端分工。
- [SPEC-004 使用者管理與登入](../../../其他/使用者管理與登入/spec.md)：既有使用者↔角色指派資料，用以判定「是否已指派」。
- SPEC-016 系統參數維護：維護清單預設每頁筆數取自「查詢列表分頁筆數」（2026-10-06 該參數移除，改為固定 10，ADR-007）。

## Risks（風險）

- **無防鎖死**：管理者可取消所有角色的 `rolePermission`，導致介面無人可管理角色，須人工改資料表恢復（依 SPEC-005 FR-005 之運維取捨）。
- **`isAssigned` 全量讀取**：以單次讀取全部使用者彙總，使用者規模顯著成長時需改為只投影 `RoleId` 的彙總查詢（R4）。
- **自動產碼撞碼**：極端併發下可能撞碼，由唯一索引攔截並回報錯誤（R5，機率極低）。
- **前端對接依賴**：設定權限的並行控制要求前端回帶 `rowVersion`（`ETag`），且勾選識別碼須由字串 `value` 改為整數 `MenuItemId`；前端由另一團隊串接。

## 實作紀錄（Archive）

- **狀態**：已實作並收斂，所有任務（T001–T039）完成；T036（刪除支援選填 rowVersion）經釐清 2026-09-29 定案**不實作**。
- **主要提交**：`7f1f6dc` 規格 → `ff34547` 技術設計與契約 → `083eb92` 任務清單 → `f2fb629` 釐清（授權字串、並行必填、移除搜尋）→ `faaf8ac` 實作 → `e55ef79` 分析後文件對齊 → `8b2124b` 移除 `IsContainer` 欄位（2026-09-22）→ `22063dc`、`29cd6d1` 收斂 T029–T035（授權收斂、請求驗證、產碼不回收、409 通用訊息）→ `3068f7a`、`ead7371` 收斂 T036 與刪除不做並行檢查 → `b95bed6`、`0ab0035` 釐清（新增僅限 Custom、停用者計入已指派、撤回 FR-018 防鎖死）→ `c24e715`、`29122df`、`99ec740` 收斂 T037–T039（新增僅接受 Custom）→ `0f49a86` 修正分析列出的文件落差 → `f27e80f` 併入主線（隨 014 合併）→ `fa91926` 預設每頁筆數改取系統參數（SPEC-016）→ `c8e5872` 移除舊後台選單（2026-10-02）。
- **收斂紀錄**：T029 授權由 `MenuAccess:rolePermission|/admin/roles` 收斂為 `rolePermission`；T030 `POST /roles` 補請求驗證（原空白／過長名稱擲例外成 500）；T031 移除恆真斷言並補分頁、超頁、未指派情境；T032 產碼跨軟刪除掃描；T033 全域 409 訊息改通用文字；T034 補空清單測試；T035 文件／註解對齊；T037–T039 新增角色僅接受 Custom。
- **測試數**：原任務清單未記錄本規格測試數。
- **審查紀錄**：規格品質檢查（原 `checklists/requirements.md`）全數通過，於 2026-09-21 核准進入技術設計；三項關鍵決策（FR-004 角色種類新增欄位、FR-007 沿用 Code 並由後端產碼、FR-012 已指派刪除保護後端強制）於規格建立階段拍板。
- **原 Spec Kit 產出**：plan.md、research.md、data-model.md、quickstart.md、tasks.md、contracts/、checklists/ 已併入本檔後移除；原始檔可由 `git show c8e5872:"specs/管理業務/系統管理作業/角色權限維護/tasks.md"` 等方式追溯。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：2026-09-30 釐清修訂的紅燈 commit：`bae716a`（T037）。初版實作沒有單獨提交紅燈 commit，無紀錄可補。
