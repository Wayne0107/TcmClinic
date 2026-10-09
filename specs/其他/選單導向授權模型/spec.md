# [SPEC-005] Feature: 選單導向的授權模型（移除超級管理員與防鎖死）

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-005 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 無（跨選單基礎規格） |
| 建立日期 | 2026-08-31 |
| 最後更新 | 2026-10-04（由 Spec Kit 格式轉為 UDS SDD 格式；內容未變） |
| 原分支 | `005-menu-based-authorization` |
| 相關規格 | 取代 SPEC-002 後台選單與角色權限管理 FR-014／FR-017、SPEC-004 使用者管理與登入 FR-018／FR-019／FR-022；被 SPEC-012 角色權限維護修改（角色維護 4 支端點守門鍵由 `/admin/roles` 改為 `rolePermission`）；被 SPEC-014 分行資料維護修改（FR-006a 新增 `/branches/options` 僅需登入之例外）；SPEC-009 開發模式可見全部選單擴充 SUPERADMIN 可見性；SPEC-007 CHB選單與角色種子、SPEC-008 選單API欄位對齊前端建於本規格可見選單機制 |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

> **狀態沿革**：Implemented（2026-09-01）；2026-10-01 收斂 T033–T035；2026-10-02 移除舊後台選單（見 Clarifications Session 2026-10-02）。
>
> ⚠️ **2026-10-02 舊後台選單移除**：使用者管理守門鍵改 `accountMaintenance`（帳號維護）、`GET /roles` B 方案改 `accountMaintenance` **或** `rolePermission`、`PUT /roles/{id}` 改 `rolePermission`；選單管理維持 `/admin/menus`（暫不處理）；`GET /audit-logs`、`GET /access-logs` 查詢 API 已移除。下文各處保留原設計之 `/admin/users`、`/admin/roles` 字樣，與本註記衝突處以本註記為準。
>
> ⚠️ **後續修改**：SPEC-012 角色權限維護（2026-09-21 釐清）將 `POST /roles`、`DELETE /roles/{id}`、`GET /roles/{id}/menu-visibility`、`PUT /roles/{id}/menu-visibility` 的守門鍵由 `/admin/roles` 改為 `rolePermission`；SPEC-014 分行資料維護 FR-019(b) 新增 `GET /branches/options` 為第二個跨頁共用查詢（僅需登入）。

## Overview（概述）

把後台授權從「固定超級管理員角色守門」改為「角色對選單的可見性＝該頁面所有功能的操作權」：使用者（經其一或多個角色）對某頁面選單有可見性，即可使用該頁面的檢視／新增／編輯／刪除；無可見性者連讀取亦回 403（對稱模型）。同時移除「超級管理員」的特殊地位與所有防鎖死攔截，若設定不當導致無人可管理，由具資料庫存取權者直接修改資料表恢復。

原始需求（使用者描述）：「拿掉超管，不用防鎖死設計（若遇到鎖死我直接動資料表就好）；資料表有對應的選單就要有對應選單的所有功能。」

## Motivation（動機）

- **US1（P1）**：這是本功能的核心價值——用單一「選單可見性」資料同時決定「看得到」與「能操作」，取代「只有超級管理員能操作」的舊模型。沒有它，其餘變更都無意義。
- **US2（P2）**：移除硬編碼的超管守門是本模型成立的前提；否則授權仍會被超管特例綁架。依賴 US1 之授權機制到位。
- **US3（P3）**：明示的運維取捨——以簡化模型換取「不需維護防鎖死邏輯」；發生機率低且可人工恢復。

## Clarifications（釐清紀錄）

### Session 2026-08-31

- Q: 授權的細粒度為何？ → A: 不分檢視／編輯；角色對某選單（頁面）有可見性，即擁有該頁面的**所有功能**（檢視／新增／編輯／刪除）。
- Q: 拿掉超管後如何防止「無人可管理」的鎖死？ → A: **不做防鎖死**。若發生，由具資料庫存取權者直接修改資料表恢復（明示接受之運維取捨）。
- Q: 「超級管理員」角色是否仍特殊？ → A: 否；改為一般角色，可被編輯／刪除，不再有專屬守門或保護。
- Q: 跨頁面的共用唯讀查詢（如「新增使用者」表單所需的角色清單）如何授權？ → A（2026-08-31 修訂為**對稱模型**）: **讀取（查詢）亦依選單可見性授權**——有可見性才可讀該頁資料、含頁面本身之查詢；無可見性者**讀取亦回 403**。唯一之跨頁共用查詢 `GET /roles`（供「新增使用者」表單載入角色清單）採 **B 方案**：具 `/admin/users` **或** `/admin/roles` 任一可見性者即可讀。（此決議取代先前「讀取全開放」之版本。）
- Q: 首次部署是否以種子資料自動授予初始角色所有選單可見性？ → A（2026-09-01 修訂）: 由**種子**建立三個內建管理選單（使用者管理／角色權限管理／選單管理）並授予種子管理角色（admin）其可見性，使 admin **登入後即可管理**（開箱即用）；其餘角色/選單之可見性仍由管理者設定。（取代先前「完全不使用種子資料」之版本。）

### Session 2026-10-02

背景：CHB 選單已併入規格（見 SPEC-007 CHB選單與角色種子），範本早期的舊後台選單（`/admin/users`、`/admin/roles`、`/admin/audit-logs`、`/admin/access-logs`）不再使用。使用者決定直接移除並修訂相關規格，不另開規格。

- Q: 舊後台選單移除後，各端點的授權鍵如何調整？ → A: 使用者管理 → `accountMaintenance`（帳號維護）；跨頁共用查詢 `GET /roles`（B 方案）→ `accountMaintenance` **或** `rolePermission` 任一可見即可；`PUT /roles/{id}` → `rolePermission`；選單管理維持 `/admin/menus`（暫不處理）。資料異動紀錄、資料訪問紀錄的查詢 API 一併移除（見各該規格），寫入不變。
- Q: 2026-09-01「種子建立三個內建管理選單並授予 admin」是否仍適用？ → A: 僅保留「選單管理」（`/admin/menus`）；其餘舊後台選單由種子軟刪除，admin 於開發模式改經 SUPERADMIN 可見全部選單取得使用者與角色維護的存取。

## Requirements（需求）

> 本功能改寫既有後台的「授權模型」：從「固定超級管理員角色守門」改為「角色對選單的可見性＝該選單頁面所有功能的操作權」。取代 SPEC-002 與 SPEC-004 中以超級管理員與防鎖死為核心的存取控制需求（見 Dependencies）。

### Requirement: US1 依選單可見性授權頁面所有功能（P1）

The system SHALL 以角色對具路由選單的可見性作為頁面所有功能（讀取與寫入）的唯一授權依據，多角色取聯集，無可見性回 403。

已登入使用者只能使用「其角色被授權可見之選單」所對應頁面的功能。當使用者（透過其一或多個角色）對某選單（頁面）有可見性時，即可執行該頁面的所有功能（檢視、新增、編輯、刪除）；對無可見性的頁面，其功能一律被拒。獨立驗證方式：建立一個角色，只授權其可見「使用者管理」選單，指派給某使用者；以該使用者登入後，應能完整操作使用者管理（查詢／新增／編輯／刪除），但存取「角色權限管理」「選單管理」之功能一律被拒。涵蓋 FR-001、FR-002、FR-006、FR-006a、FR-007。

#### Scenario: AC-1 有可見性即可使用該頁所有功能
- **GIVEN** 某角色被授權可見「使用者管理」選單
- **WHEN** 該角色的使用者呼叫使用者管理的任一功能（查詢／新增／編輯／刪除）
- **THEN** 系統允許執行

#### Scenario: AC-2 無可見性即拒絕
- **GIVEN** 某角色未被授權可見「角色權限管理」選單
- **WHEN** 該角色的使用者嘗試存取角色管理功能
- **THEN** 系統拒絕並回應「無權限」（HTTP 403）

#### Scenario: AC-3 多角色取聯集
- **GIVEN** 具多個角色的使用者
- **WHEN** 判定其可操作的頁面
- **THEN** 為其各角色可見選單的聯集

#### Scenario: AC-4 未登入導向登入
- **GIVEN** 未登入的存取者
- **WHEN** 存取任一受控頁面功能
- **THEN** 系統導向登入頁（沿用既有登入機制）

### Requirement: US2 移除超級管理員的特殊地位（P2）

The system SHALL 將「超級管理員」視為一般角色，可被編輯與刪除，且不保留任何僅超級管理員可存取之政策。

「超級管理員」角色不再是受保護、不可刪除、專屬守門的角色；它與其他角色一視同仁，其權限一律由「它被授權可見哪些選單」決定。獨立驗證方式：對原「超級管理員」角色執行編輯與刪除，系統應允許（不再阻擋）；其使用者的可操作範圍應僅由其角色的選單可見性決定，而非「因為是超管所以全能」。涵蓋 FR-003、FR-004。

#### Scenario: AC-5 超級管理員可編輯與刪除
- **GIVEN** 原「超級管理員」角色
- **WHEN** 管理者編輯其名稱或刪除它
- **THEN** 系統允許（不再回覆「不可刪除／不可停用」）

#### Scenario: AC-6 無超管特例政策
- **GIVEN** 系統中不存在任何專屬「超級管理員」政策
- **WHEN** 判定任一頁面之存取權
- **THEN** 一律以選單可見性（US1）決定，無超管特例

### Requirement: US3 不設防鎖死，接受人工恢復（P3）

The system SHALL 不阻擋任何可能導致「無人可管理」的權限設定操作，恢復途徑為直接修改資料表。

若因設定造成無人能進入角色／選單管理，恢復方式為由具資料庫存取權者直接修改資料表（非系統內建功能）。獨立驗證方式：移除所有角色對「角色權限管理」選單的可見性並儲存，系統應允許（不阻擋）；此後無人能於介面管理權限，須由 DBA 直接改資料表恢復。涵蓋 FR-005、FR-008。

#### Scenario: AC-7 不攔截導致無人可管理的操作
- **GIVEN** 任一會使「無角色可見角色/選單管理」的操作
- **WHEN** 管理者送出
- **THEN** 系統允許執行、不阻擋（無防鎖死攔截）

#### Scenario: AC-8 以直接修改資料表恢復
- **GIVEN** 因權限設定導致介面無人可管理
- **WHEN** 需恢復
- **THEN** 透過直接修改資料表（`SysRoles`／`SysRoleTasks`／`SysUserRoles`）恢復，系統不提供內建解救流程

> 表名註記：SPEC-002（2026-09-30 修訂）已將表名改為單數 `SysRole`／`SysTask`／`SysRoleTask`，SPEC-004 將 `SysUserRoles` 改名 `SysUserRole`；本規格原文保留當時表名。

### 功能需求條文（Functional Requirements）

- **FR-001**: 系統 MUST 以「角色對選單（具路由之頁面）的可見性」作為頁面功能的授權依據：使用者透過其一或多個角色對某頁面之選單有可見性時，MUST 允許其使用該頁面之**所有功能**（檢視／新增／編輯／刪除）；無可見性時 MUST 拒絕並回應 HTTP 403。
- **FR-002**: 多角色使用者之可操作範圍 MUST 為其各角色可見選單之**聯集**。
- **FR-003**: 系統 MUST 移除「超級管理員」角色的專屬守門地位；所有受控頁面之存取一律由 FR-001 決定，系統 MUST NOT 保留任何「僅超級管理員可存取」之政策。（取代 SPEC-004 FR-018、SPEC-002 FR-017）
- **FR-004**: 「超級管理員」角色 MUST 可如一般角色被編輯與刪除，MUST NOT 再被視為受保護、不可刪除或不可停用之角色。（取代 SPEC-002 FR-014／FR-017 之保護）
- **FR-005**: 系統 MUST NOT 實作任何「防鎖死」阻擋；即使某操作將導致無任何角色可見「角色權限管理」或「選單管理」，系統仍 MUST 允許之。（取代 SPEC-004 FR-019／FR-022、SPEC-002 FR-017 之防鎖死）
- **FR-006**: 每個受控頁面（功能集合）MUST 對應到一個具路由的選單項目，作為授權判定單位；不具對應選單之端點（登入、登出、取得自身身分與自身可見選單）MUST 對任何已登入使用者開放，登入端點維持公開。
- **FR-006a**: 授權採**對稱模型**——頁面之**讀取（查詢）與寫入（新增／修改／刪除）皆**依 FR-001 之選單可見性判定；無可見性者存取該頁功能（含讀取）MUST 回 403。**例外（B 方案）**：跨頁共用查詢 `GET /roles`（供「新增使用者」表單載入角色清單）MUST 允許「具 `/admin/users` **或** `/admin/roles` 任一可見性」者讀取。此為目前唯一之跨頁共用查詢；其餘端點一律「頁面＝選單＝API」單一對應。（**後續例外**：SPEC-014 分行資料維護 FR-019(b) 新增 `GET /branches/options`（分行選項清單，供多個頁面的表單使用）為第二個跨頁共用查詢，**僅需登入**、不依選單可見性；使用者於 2026-10-01 確認承認此例外。）（⚠️ **2026-10-02**：`/admin/users`、`/admin/roles` 移除，B 方案改為具 `accountMaintenance`（帳號維護）**或** `rolePermission`（角色權限維護）任一可見性者即可讀；見 Clarifications Session 2026-10-02。）
- **FR-007**: 未登入者存取任一受控頁面功能時，系統 MUST 導向登入（沿用既有登入機制）。
- **FR-008**: 因權限設定導致介面無人可管理時，系統之恢復途徑 MUST 為「直接修改資料表」；系統 MUST NOT 需提供內建之解救／自動恢復流程。

### 邊界情境（Edge Cases）

- **無對應選單的端點**：登入、登出、取得自身身分與自身可見選單等，不對應任何選單項目；這些對「任何已登入使用者」開放（登入本身為公開）。
- **具路由子項 vs 容器**：授權以「具路由的選單子項（實際頁面）」為單位；純容器父項不代表可操作任何功能。
- **多角色聯集**：使用者權限為其各角色可見選單之聯集；任一角色可見即可操作。
- **登入中權限變更**：角色的選單可見性變更後，於該使用者下次載入頁面即生效（沿用既有每請求重驗機制）。
- **首次部署**：種子建立三個管理選單並授予 admin 可見性，admin 登入後即可管理（開箱即用）。若日後所有角色之管理頁可見性被移除而無人可管理，因不設防鎖死，須由人工改資料表恢復。（⚠️ 2026-10-02 起種子僅保留 `/admin/menus`，見 Clarifications Session 2026-10-02。）

### 關鍵實體（Key Entities）

- **角色（Role）**：權限主體；沿用既有結構，但「超級管理員」不再具特殊保護，與其他角色一視同仁。
- **選單項目（具路由子項）**：對應一個實際頁面及其**所有功能**；其可見性即該頁面功能之操作授權單位。純容器父項不授予功能。
- **角色選單可見性（Role Menu Visibility）**：角色與選單項目之可見對應；本功能將其由「僅決定看得到」升級為「同時決定看得到與能操作」的**單一授權來源**。
- **使用者（User）**：透過其所屬角色取得可見選單之聯集，即其可操作的頁面功能集合。

### 假設（Assumptions）

- 規格內容依憲章 Principle VIII 以繁體中文撰寫；`FR-###`／`SC-###`／`P1` 等標記保留原樣作為規格結構標記。
- **可見性＝所有功能**：本次不區分「檢視／編輯／刪除」之細粒度權限；角色對某頁面選單有可見性即擁有該頁面全部動作。細粒度動作權限不在本次範圍。
- **不設防鎖死**：明示接受「介面可能被設定成無人可管理」之風險，恢復以直接修改資料表為之；此為使用者明確指定之取捨。
- **驗證不變、僅改授權**：登入／登出／工作階段（Cookie）沿用 SPEC-004；本功能僅改「授權」而非「驗證」。角色變更於下次載入生效之機制沿用既有每請求重驗。
- **種子提供 admin 初始可見性（開箱即用）**：種子建立三個內建管理選單並授予種子管理角色（admin）其可見性，使 admin 登入後即可管理。其餘角色之可見性由管理者於介面設定；原「超級管理員」角色視為一般角色（僅初始被授予可見性，無授權特權）。仍**不設防鎖死**（FR-005/008），設定不當導致無人可管理時，由人工改資料表恢復。
- **頁面↔選單對應**：每個受控頁面以其路由對應一個選單項目；實際的「端點 ↔ 選單」對應關係由技術設計定義（見 Technical Design › 設計決策 R1、API 契約），本規格需求段僅規範授權語意。

## Acceptance Criteria（驗收條件）

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、SC-001 | 具某頁選單可見性者可成功執行該頁 100% 功能（查詢／新增／編輯／刪除） | `backend/tests/TcmClinic.Api.FunctionalTests/MenuAuthorizationTests.cs` › `Users_menu_grants_full_user_management` |
| AC-2 | US1 情境 2、SC-002、FR-006a | 無可見性者存取該頁功能（含讀取）100% 回 403；`GET /roles` 依 B 方案任一可見即可讀 | `MenuAuthorizationTests` › `Roles_write_forbidden_but_list_readable_via_B`；`backend/tests/TcmClinic.Api.UnitTests/Authorization/MenuAccessHandlerTests.cs` › `Fails_when_menu_claim_absent`、`Succeeds_when_any_of_required_routes_matches` |
| AC-3 | US1 情境 3、SC-005、FR-002 | 多角色使用者可操作頁面集合 100% 等於各角色可見選單聯集（無多授、無漏授） | `MenuAuthorizationTests` › `Union_of_visibility_grants_multiple_pages`；`backend/tests/TcmClinic.Api.UnitTests/Authorization/GetVisibleMenuRoutesTests.cs` › `Union_excludes_container_and_ungranted` |
| AC-4 | US1 情境 4、FR-007 | 未登入存取受控端點回 401（前端導向登入） | `backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs` › `Protected_endpoint_without_login_returns_401` |
| AC-5 | US2 情境 1、SC-003、FR-004 | 原超級管理員角色可被編輯與刪除，種子不宣稱其不可刪除 | `backend/tests/TcmClinic.Api.IntegrationTests/Data/RoleTypeSeedTests.cs` › `Super_admin_description_does_not_claim_undeletable`；手動：Test Plan 手動走查第 4 步（改名） |
| AC-6 | US2 情境 2、FR-003 | 系統不存在任何僅超級管理員可存取之政策 | 手動：Code Review 確認 `backend/src` 中 `SuperAdminPolicy`、`IsSuperAdmin` 出現次數為 0 |
| AC-7 | US3 情境 1、SC-004、FR-005 | 防鎖死攔截次數為 0：清空角色可見性、刪除唯一管理帳號皆被允許 | `MenuAuthorizationTests` › `Clearing_visibility_is_allowed_no_antilockout`；`backend/tests/TcmClinic.Api.FunctionalTests/UserCrudEndpointsTests.cs` › `Delete_last_superadmin_succeeds_no_antilockout` |
| AC-8 | US3 情境 2、FR-008 | 無人可管理時以直接修改資料表恢復，系統不提供內建解救流程 | 手動：Test Plan 手動走查第 5 步 |
| AC-9 | FR-006 | 不對應選單之端點（`/me/menus`）對任何已登入者開放；其他受控端點無可見性即 403 | `MenuAuthorizationTests` › `No_visibility_forbids_reads_but_me_menus_open` |
| AC-10 | Edge Case「登入中權限變更」、FR-002 | 角色可見性於登入中變更後，使用者下次請求即反映（`menu` claims 每請求刷新） | `backend/tests/TcmClinic.Api.FunctionalTests/MenuClaimsRevalidationTests.cs` › `Visibility_change_reflected_in_menu_claims_next_request` |
| AC-11 | Clarifications 2026-09-01、Edge Case「首次部署」 | 種子 admin 登入後即見管理選單且可存取各頁（開箱即用） | `AuthEndpointsTests` › `Seeded_admin_sees_menus_and_can_access` |

> AC-1、AC-2、AC-3、AC-9、AC-10 的測試自 2026-10-02 起以 `accountMaintenance`／`rolePermission` 取代 `/admin/users`／`/admin/roles` 作為授權鍵（見頂端註記）。
>
> AC-5 的「刪除」部分自 SPEC-012 角色權限維護 FR-012 起受「已指派不可刪」規則約束：種子 admin 已指派 SUPERADMIN，故刪除回 400（`backend/tests/TcmClinic.Api.FunctionalTests/RoleCrudEndpointsTests.cs` › `Delete_assigned_superadmin_returns_400`）；此為一般角色共通規則，非超管特例，不違反 FR-004。

## Technical Design（技術設計）

### 技術脈絡

- 語言／框架：C# / .NET 10（`global.json` SDK 10.0.201、TFM `net10.0`）。
- 相依（皆既有，沿用）：FastEndpoints、EF Core、Mediator、Ardalis.Result／Specification、Vogen、ASP.NET Core Cookie Authentication、`Microsoft.AspNetCore.Authorization`（自訂 `IAuthorizationPolicyProvider`／`IAuthorizationHandler`，框架內建，非新增套件）。
- 儲存：EF Core → SQL Server／SQLite。**不新增資料表**：`SysRoleTasks`（角色↔選單可見性）直接作為授權來源；`SysTasks.Route` 作為授權鍵。
- 測試：xUnit v3、NSubstitute、Testcontainers、`WebApplicationFactory`；Verify（授權後之端點契約快照）；NetArchTest。每個 `[Fact]`/`[Theory]` 帶 zh-TW `DisplayName`（憲章 Principle X）。
- 效能目標（沿用預設，Principle XIV）：查詢 ≤ 300ms p95、寫入 ≤ 500ms p95。授權判定於每請求以 claims 比對（O(1)～O(可見選單數)），可見選單於 `OnValidatePrincipal` 每請求聚合一次（沿用既有每請求 DB 重驗，成本可接受）。
- 規模：後台管理者規模（數十～數百使用者）；本功能為模板參考實作。
- 約束：
  - 授權鍵為選單之 `Route`（前端路由字串）；受控端點以其守門頁的 route 常數宣告授權。管理頁的 route 為程式碼固定之標準路由，不得任意更名，否則授權與端點對應會漂移（記於設計決策 R1）。
  - **對稱模型**（FR-006a）：讀取（GET）與寫入（POST/PUT/PATCH/DELETE）**皆**依守門選單可見性授權；唯一例外為 `GET /roles` 之 B 方案。
  - **無防鎖死**（FR-005/008）：不實作任何防鎖死攔截。**種子初始可見性**：由種子建立內建管理選單並授予種子管理角色（admin）其可見性（開箱即用）；其餘角色/選單之可見性仍由管理者設定。

技術取向：沿用既有 `RoleMenuVisibility`（表 `SysRoleTasks`）作為**單一授權來源**、沿用既有 Cookie 驗證與每請求重驗機制（`OnValidatePrincipal`，SPEC-004 設計決策 R8）——於重驗時**額外把使用者可見的選單路由寫入 claims**；以**自訂授權原則供應器**（`IAuthorizationPolicyProvider`）讓端點以 `Policies("MenuAccess:<route>")` 宣告其守門選單。

### 受影響元件

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.Core/RoleAggregate/Role.cs` | 移除 `IsSuperAdmin`／`SuperAdminCode` 特例（降為一般角色） |
| `backend/src/TcmClinic.Api.UseCases/Auth/GetVisibleMenuRoutes.cs` | 新增：`GetVisibleMenuRoutesQuery` + Handler，依角色代碼回傳可見「具路由選單」之 route 集合（`IReadOnlySet<string>`） |
| `backend/src/TcmClinic.Api.UseCases/Roles/DeleteRole.cs` | 移除「`SUPERADMIN` 不可刪除」檢查 |
| `backend/src/TcmClinic.Api.UseCases/Users/DeleteUser.cs` | 移除「不可刪最後超管／刪除自己」防鎖死檢查 |
| `backend/src/TcmClinic.Api.UseCases/Users/UpdateUser.cs` | 移除「不可移除自身超管角色」防鎖死檢查 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/SeedData.cs` | `SUPERADMIN` 種為一般角色（說明不宣稱不可刪除，T033）；冪等種入內建管理選單並授予 admin 可見性（開箱即用） |
| `backend/src/TcmClinic.Api.Web/Configurations/AuthConfigs.cs` | 移除 `SuperAdminPolicy`；`OnValidatePrincipal` 每請求寫入 `menu` claims |
| `backend/src/TcmClinic.Api.Web/Configurations/MenuAccessPolicyProvider.cs` | 新增：解析 `MenuAccess:{routes}`（`|` 分隔多值）→ `MenuAccessRequirement`；回退至預設供應器 |
| `backend/src/TcmClinic.Api.Web/Authorization/MenuAccess.cs` | 新增：`MenuAccessRequirement`（可接受 route 陣列）+ `MenuAccessHandler`（`menu` claims 任一命中即 `Succeed`，OR 語意，大小寫不敏感） |
| `backend/src/TcmClinic.Api.Web/Endpoints/Users/*.cs` | `CreateUserEndpoint`／`UpdateUserEndpoint`／`DeleteUserEndpoint`／`ResetPasswordEndpoint`／`GetUsersEndpoint` 改 `Policies("MenuAccess:…")`（現為 `accountMaintenance`） |
| `backend/src/TcmClinic.Api.Web/Endpoints/Roles/*.cs` | `CreateRoleEndpoint`／`UpdateRoleEndpoint`／`DeleteRoleEndpoint`／`SetRoleVisibilityEndpoint`／`GetRoleVisibilityEndpoint` 改 `MenuAccess`（現為 `rolePermission`）；`GetRolesEndpoint` 用 B 方案（現為 `MenuAccess:accountMaintenance|rolePermission`） |
| `backend/src/TcmClinic.Api.Web/Endpoints/Menus/*.cs` | `CreateMenuEndpoint`／`UpdateMenuEndpoint`／`SetMenuEnabledEndpoint`／`ReorderMenuEndpoint`／`DeleteMenuEndpoint`／`GetMenuTreeEndpoint` 改 `Policies("MenuAccess:/admin/menus")` |
| `backend/tests/TcmClinic.Api.FunctionalTests/TestAuthHandler.cs`、`CustomWebApplicationFactory.cs`、`AuthWebApplicationFactory.cs` | 測試基礎設施：`TestAuthHandler` 注入 Role + `menu` claims、`X-Test-Menus` 標頭模擬可見性 |

### 資料模型

**本功能不新增任何資料表、不變更資料表結構、無 migration。** 授權改由既有資料承載；以下說明既有實體在新模型下的角色轉變與授權判定資料流。

#### 授權來源（既有實體，語意升級）

| 實體 / 表 | 既有用途 | 在本規格的角色 |
|-----------|---------|--------------|
| **選單項目 `MenuItem`（表 `SysTasks`）** | 側邊選單節點；`Route` 為前端路由（具路由子項＝實際頁面；容器 `Route=null`） | `Route` 成為**授權鍵**；具路由子項＝一個受控頁面（其所有功能之授權單位） |
| **角色選單可見性 `RoleMenuVisibility`（表 `SysRoleTasks`）** | 決定角色看得到哪些選單 | **單一授權來源**：角色對某具路由選單有可見性 → 該角色使用者擁有該頁**寫入**功能之權 |
| **角色 `Role`（表 `SysRoles`）** | 權限主體；含受保護之 `SUPERADMIN` | **移除超管特例**：`IsSuperAdmin`/`SuperAdminCode` 不再賦予特權；`Role` 降為一般角色，可編輯/刪除 |
| **使用者角色 `UserRole`（表 `SysUserRoles`）** | 使用者↔角色 | 沿用；使用者權限＝其各角色可見選單之聯集 |

> 上表「擁有該頁**寫入**功能之權」為對稱模型修訂前的原文；依 FR-006a，可見性同時授權讀取與寫入。表名後續改為單數（見 US3 下方表名註記）。

#### 網域變更（Core）

- **`Role`（表 `SysRoles`）**：**移除** `IsSuperAdmin` 屬性與 `SuperAdminCode` 之**特殊語意**（不再被授權/防鎖死程式引用）。`"SUPERADMIN"` 字串若仍存在於資料中，視為**一般角色代碼**，不具特權。不再有「受保護、不可刪除/停用」之角色；`Role` 可被編輯與刪除（沿用既有 CRUD，移除保護檢查）。

#### 使用案例（UseCases）

- **＋ `GetVisibleMenuRoutes(roleCodes)`**：依角色代碼集合，回傳其可見之**具路由選單的 route 集合**（多角色聯集、排除容器）。輸入為角色代碼、輸出為 `IReadOnlySet<string>`；`AsNoTracking`、投影。
  - 資料流：`roleCodes` → `SysRoleTasks`（該些角色的可見選單）→ `SysTasks`（取具路由子項之 `Route`）。
  - 與既有 `GetMyMenu` 同源，惟只需 route 集合（供授權），不需整棵樹。
- **～ 移除防鎖死檢查**：`DeleteUser`（最後超管/自己）、`UpdateUser`（自身超管）、`DeleteRole`（超管不可刪）之相關防鎖死邏輯刪除。

#### 授權判定資料流（每請求）

```text
請求（帶 Cookie）
  → OnValidatePrincipal（每請求，沿用 SPEC-004 R8）
      1. 依 Name(帳號) 查現時角色代碼（既有）
      2. 以角色代碼呼叫 GetVisibleMenuRoutes → 可見 route 集合
      3. 將每個 route 寫入 ClaimsPrincipal 之 "menu" claim（既有角色 claim 亦刷新）
  → 端點授權
      - 受控端點：Policies("MenuAccess:<route>") → 檢查 "menu" claims 是否含該 route
      - 不受頁面授權端點：僅需已登入（[Authorize]）
```

- **即時性**：角色可見性變更後，於該使用者**下次請求**即反映（沿用每請求重驗）。
- **多角色**：可見 route 集合為各角色之聯集（FR-002）。

#### 授權鍵對照（頁面 ↔ route ↔ 受控端點，讀＋寫皆 gate）

| 管理頁 | 授權鍵（選單 `Route`） | 受此鍵守門之端點（讀取＋寫入皆需可見性） |
|--------|----------------------|------------------------------------------|
| 使用者管理 | `/admin/users`（2026-10-02 起 `accountMaintenance`） | `GET /users`、`POST /users`、`PUT /users/{id}`、`DELETE /users/{id}`、`PUT /users/{id}/password` |
| 角色權限管理 | `/admin/roles`（後續改 `rolePermission`，見頂端註記） | `POST /roles`、`PUT /roles/{id}`、`DELETE /roles/{id}`、`GET/PUT /roles/{id}/menu-visibility` |
| 選單管理 | `/admin/menus` | `GET /menus`、`POST /menus`、`PUT /menus/{id}`、`PATCH /menus/{id}/enabled`、`PUT /menus/reorder`、`DELETE /menus/{id}` |

**跨頁共用查詢（B 方案，OR 兩 route）**：`GET /roles` → `MenuAccess:/admin/users|/admin/roles`（具其一可見性即可讀；供新增使用者表單載入角色清單）。2026-10-02 起為 `MenuAccess:accountMaintenance|rolePermission`。

**不受頁面授權（僅需登入）**：`GET /me/menus`（回自身可見選單）、`GET /auth/me`（自身身分）。**公開**：`POST /auth/login`。**已登入即可**：`POST /auth/logout`。

無狀態機。

### API 契約

#### 原 contracts/authorization.md（API 契約：授權規則 Menu-based Authorization）

> 原檔各節無 § 編號；以下保留原節標題與順序。

本功能不改變端點的請求/回應形狀，只改變**授權**。以下規範每個端點的授權判定與回應碼。錯誤一律 RFC 7807 ProblemDetails。授權鍵＝守門選單之 `Route`（見資料模型）。

> ⚠️ **2026-10-02 舊後台選單移除**：使用者管理 5 支端點的守門鍵由 `/admin/users` 改為 `accountMaintenance`；`GET /roles`（B 方案）改為具 `accountMaintenance` **或** `rolePermission` 任一可見性者即可讀；`PUT /roles/{id}` 改為 `rolePermission`；選單管理維持 `/admin/menus`；`GET /audit-logs`、`GET /access-logs` 查詢 API 已移除。以下各表保留原設計；與本註記衝突處以本註記為準。

##### 授權判定總則

- **未登入** 存取受控端點 → **401 Unauthorized**（前端守衛導向 `/login`，沿用 SPEC-004）。
- **已登入但無守門選單可見性** 存取該頁功能（**讀取或寫入皆同**）→ **403 Forbidden**（ProblemDetails，一般性訊息）。**對稱模型**：讀寫一致依可見性授權。
- **跨頁共用查詢 `GET /roles`（B 方案）** → 具 `/admin/users` **或** `/admin/roles` 任一可見性者 **200**；皆無 → 403。
- **不受頁面授權**（`GET /me/menus`、`GET /auth/me`）→ 任何已登入者 200。
- 授權以 `ClaimsPrincipal` 之 `"menu"` claims（每請求重驗時寫入）比對，不逐次查 DB。

##### 使用者管理（守門鍵 `/admin/users`）

| 端點 | 類型 | 授權 | 回應 |
|------|------|------|------|
| `GET /users` | 讀 | 需 `/admin/users` 可見性 | 200 / 403 |
| `POST /users` | 寫 | 需 `/admin/users` 可見性 | 201 / 403 / 400 |
| `PUT /users/{id}` | 寫 | 需 `/admin/users` | 200 / 403 / 404 / 400 |
| `DELETE /users/{id}` | 寫 | 需 `/admin/users` | 204 / 403 / 404 |
| `PUT /users/{id}/password` | 寫 | 需 `/admin/users` | 204 / 403 / 404 / 400 |

> `DELETE /users/{id}`：**移除防鎖死** —— 不再阻擋「刪除最後超管/自己」（FR-005）。回 400 僅保留一般資料驗證（如不存在→404）。

##### 角色權限管理（守門鍵 `/admin/roles`）

| 端點 | 類型 | 授權 | 回應 |
|------|------|------|------|
| `GET /roles` | 讀（跨頁共用） | **B**：需 `/admin/users` **或** `/admin/roles`（供新增使用者表單，FR-006a） | 200 / 403 |
| `GET /roles/{id}/menu-visibility` | 讀 | 需 `/admin/roles` 可見性 | 200 / 403 |
| `POST /roles` | 寫 | 需 `/admin/roles` | 201 / 403 / 400 |
| `PUT /roles/{id}` | 寫 | 需 `/admin/roles` | 200 / 403 / 404 / 400 |
| `DELETE /roles/{id}` | 寫 | 需 `/admin/roles` | 204 / 403 / 404 |
| `PUT /roles/{id}/menu-visibility` | 寫 | 需 `/admin/roles` | 204 / 403 / 404 / 400 |

> `DELETE /roles/{id}`：**移除超管保護** —— 原 `SUPERADMIN` 角色可被刪除（FR-004），不再回「不可刪除」。
> `PUT /roles/{id}/menu-visibility`：**無防鎖死** —— 允許移除所有角色對 `/admin/roles` 之可見性（FR-005），即使結果為介面無人可管理。
>
> ⚠️ **後續修改（SPEC-012 角色權限維護釐清 2026-09-21）**：`POST /roles`、`DELETE /roles/{id}`、`GET /roles/{id}/menu-visibility`、`PUT /roles/{id}/menu-visibility` 的守門鍵已由 `/admin/roles` 改為 `rolePermission`（對齊 CHB.Web 選單 value），並新增 `GET /roles/maintenance`（同為 `rolePermission`）；`GET /roles`（B 方案）與 `PUT /roles/{id}` 維持本表（2026-10-02 再依頂端註記調整）。詳見 [SPEC-012 角色權限維護](../../管理業務/系統管理作業/角色權限維護/spec.md) 的「Technical Design › API 契約」。
>
> ⚠️ **後續例外（SPEC-014 分行資料維護 FR-019(b)，使用者 2026-10-01 確認）**：`GET /branches/options`（分行選項清單）為第二個跨頁共用查詢，僅需登入、不依選單可見性（見 FR-006a）。

##### 選單管理（守門鍵 `/admin/menus`）

| 端點 | 類型 | 授權 | 回應 |
|------|------|------|------|
| `GET /menus` | 讀 | 需 `/admin/menus` 可見性 | 200 / 403 |
| `POST /menus` | 寫 | 需 `/admin/menus` | 201 / 403 / 400 |
| `PUT /menus/{id}` | 寫 | 需 `/admin/menus` | 200 / 403 / 404 / 400 |
| `PATCH /menus/{id}/enabled` | 寫 | 需 `/admin/menus` | 204 / 403 / 404 |
| `PUT /menus/reorder` | 寫 | 需 `/admin/menus` | 204 / 403 / 400 |
| `DELETE /menus/{id}` | 寫 | 需 `/admin/menus` | 204 / 403 / 404 |

##### 導覽與驗證（非受控 / 公開 / 已登入即可）

| 端點 | 授權 | 說明 |
|------|------|------|
| `POST /auth/login` | 公開 | 沿用 SPEC-004 |
| `POST /auth/logout` | 已登入 | 沿用 SPEC-004 |
| `GET /auth/me` | 已登入 | 回身分/角色 |
| `GET /me/menus` | 已登入 | 回該使用者可見選單（沿用 `GetMyMenu`；本即依可見性） |

##### 契約快照（Principle XV）

以 Verify 對 `/users`、`/roles`、`/menus` 之 OpenAPI 與代表性回應（含 401/403）建立/更新快照；授權碼或回應形狀變更須使快照失敗並於 PR 重新核可。

### 設計決策（原 research）

> 決策編號沿用原研究文件，供程式碼註解追溯。

#### R1 端點如何對應到選單以判定授權
- **決定**：以選單之 **`Route`（前端路由字串，存於 `SysTasks.Route`）作為授權鍵**。三個內建管理頁有固定標準 route：`/admin/users`、`/admin/roles`、`/admin/menus`。每個**寫入端點**以常數宣告其守門選單 route（如 `POST /users` → `/admin/users`）。判定方式：使用者可見的選單 route 集合中是否含該端點宣告的 route。機制：
  1. 於 Cookie 驗證的 `OnValidatePrincipal`（沿用 SPEC-004 R8 之每請求重驗）**額外**：以使用者現時角色查其可見的**具路由選單**，將每個 route 以 claim（型別如 `"menu"`）寫入 `ClaimsPrincipal`。
  2. 自訂 `IAuthorizationPolicyProvider`：對名稱形如 `"MenuAccess:/admin/users"` 的原則，動態產生「`RequireClaim("menu", "/admin/users")`」之原則（搭配 `MenuAccessRequirement` + Handler）。
  3. 寫入端點以 `Policies("MenuAccess:/admin/users")` 宣告；讀取端點僅需登入（已由 R2 對稱模型修訂為讀取亦需可見性）。
- **理由**：
  - **零資料表變更**：直接用既有 `SysRoleTasks`（可見性）+ `SysTasks.Route`，對齊「資料表有對應選單就有功能」。可見性成為**單一授權來源**。
  - 沿用既有每請求重驗（R8）與 `GetMyMenu` 的聚合邏輯，整合面最小；claims 化後授權判定為 O(1) 比對。
  - 動態原則供應器讓「頁面 route → 原則」免逐一手動註冊，且可延伸至未來管理頁。
- **替代方案**：
  - **新增 `PermissionKey` 欄位於選單**：更穩定（route 可改名而 key 不變），但擴張資料結構、非必要；且違背「用既有選單」之取向。否決（改以「route 為標準不可任意更名」之約束替代）。
  - **後端 route 前綴約定對應**（`/users` → `/admin/users`）：脆弱、難涵蓋 `/users/{id}/password` 等；否決。
  - **每次請求即時查 DB 判斷**（不 claims 化）：可行但每端點多一次查詢；claims 化一次聚合更省。否決。
- **約束/風險**：授權鍵＝route，故三個管理頁的 route 為**標準、不可任意更名**（更名將使端點常數與可見性 route 不符而失效）。以測試護欄（授權矩陣）偵測回歸。

#### R2 讀寫皆依可見性授權（對稱模型）+ 跨頁共用查詢 B 方案（FR-006a，2026-08-31 修訂）
- **決定**：
  - **讀取（GET）與寫入（POST/PUT/PATCH/DELETE）皆**依守門選單可見性授權（`MenuAccess:{route}`）。無可見性者連讀取亦回 403。
  - **唯一例外（B 方案）**：跨頁共用查詢 `GET /roles`（供「新增使用者」表單載入角色清單）以 `Policies("MenuAccess:/admin/users|/admin/roles")` 宣告——具兩者之**任一**可見性即可讀。
  - 機制：`MenuAccessRequirement` 持有「可接受 route 陣列」，Handler 於使用者 `menu` claims 中**任一命中即 `Succeed`**（OR 語意）；原則名以 `|` 分隔多 route（如 `MenuAccess:/admin/users|/admin/roles`），由 `MenuAccessPolicyProvider` 解析。單一頁面之端點則為單一 route。
- **理由**：
  - 使用者要求「呼叫 API 之後要能設定哪些角色可**讀取**資料」→ 讀取須受權限控管（對稱模型）。
  - 盤點前端每頁對 API 之呼叫後確認：**除 `GET /roles` 外，所有端點皆「頁面＝選單＝API」單一對應**；唯一跨頁依賴是「新增使用者」表單借用角色清單。故只需對**一個端點**採 B 方案（OR 兩 route），其餘一刀切，模型乾淨。
  - B（OR 兩 route）與嚴格單 route 共用同一套機制（原則名 `A|B`），**零特例程式碼**。
- **替代方案**：
  - **讀取全開放**（前一版本）：低權限者可直接呼叫 `GET /users`/`GET /roles` 讀全部資料——使用者改為要求讀取須授權，故否決此版本。
  - **A（另立輕量「角色選項」開放端點）**：多一個端點與 DTO；B 以既有 `GET /roles` 加一個 route 即達成，較省。否決。
  - **C（嚴格單 route）**：僅具 `/admin/users` 者新增使用者時載入不到角色清單、流程受阻。否決。

#### R3 種子與初始可見性（FR-005/008、釐清 Q2）
> ⚠️ 本決策已被 Clarifications 2026-09-01 修訂（種子提供 admin 初始可見性，開箱即用；實作見原任務 T031–T032），再被 2026-10-02 修訂（僅保留 `/admin/menus`）。以下保留原決策內容供追溯。
- **決定**：**不種入任何選單可見性**。既有種子仍建立可登入的初始帳號與其角色，但：原 `SUPERADMIN` 角色**降為一般角色**（移除 `IsSuperAdmin`/`SuperAdminCode` 特例與保護）；**不**為任何角色種入 `SysRoleTasks` 可見性。初次部署後，該帳號可登入但**無任何選單可見、無法寫入**，直到有人**以人工修改 `SysRoleTasks`** 授予可見性（與「不設防鎖死、遇到就動資料表」一致）。
- **理由**：忠實遵循釐清 Q2「不使用種子資料，初始可見性人工設定」。保留登入帳號種子以維持「可登入」；不保留超管特例與可見性種子以貫徹新模型。
- **替代方案**：
  - 種子自動授予初始角色全部選單可見性（開箱即用）：使用者明確否決（釐清 Q2；後於 2026-09-01 改採）。
  - 完全移除帳號種子：會使無法登入，須先手動插入 user；過度。保留最小「可登入」種子較務實。
- **維運備註**：初次啟用本模型的資料庫，需人工插入一列（或數列）`SysRoleTasks` 授予某角色對 `/admin/roles` 之可見性，方能於介面管理其餘權限；否則一切由資料表操作。

#### R4 「移除超管與防鎖死」之範圍
- **決定**：移除下列（依 grep 盤點）：
  - `Role.IsSuperAdmin`、`Role.SuperAdminCode` 之**特殊語意**（保護、守門）；`SUPERADMIN` 字串可保留為一個普通角色代碼，但不再被程式賦予特權。
  - `AuthConfigs.SuperAdminPolicy` 與所有端點的 `Policies(SuperAdminPolicy)` → 改為 `MenuAccess:*`。
  - **防鎖死攔截**：`DeleteUser`（不可刪最後超管/自己）、`UpdateUser`（不可移除自身超管）、`DeleteRole`（超管角色不可刪）之相關檢查。
  - `SeedData` 之超管保護語意。
  - 依賴 `SuperAdminCode` 的 Specification（`UserSpecs` 中如有）。
- **理由**：對齊 FR-003/004/005（移除超管特例與防鎖死）；使授權「唯一來源＝選單可見性」。
- **替代方案**：保留 `SUPERADMIN` 為「隱性全可見」的特例 → 等同超管復活，與規格矛盾。否決。

#### R5 測試策略
- **決定**：
  - **Unit**：`GetVisibleMenuRoutes`（依角色代碼回傳可見 route 集合，含容器排除/多角色聯集）；`MenuAccessHandler`（claims 含/不含 route → succeed/fail）。property/範例式，帶 zh-TW DisplayName。
  - **Integration**：`OnValidatePrincipal` 每請求把可見 route 寫入 claims（角色變更後下次請求即更新）。（實作時改置於 FunctionalTests，見實作紀錄。）
  - **Functional（授權矩陣，對真實 SQL）**：有可見性之角色使用者 → 該頁寫入 200/201/204；無可見性 → 403。讀取端點依對稱模型亦依可見性（原文「對任何已登入者 → 200（開放）」為對稱模型修訂前版本）。原超管角色可被刪除/編輯（不再阻擋）；任何操作無防鎖死攔截（可移除所有管理可見性而不被擋）。跨頁：只具 `/admin/users` 可見性者能新增使用者（能載入 `GET /roles` 之角色清單）。
  - **Contract（Verify）**：更新 `/users`、`/roles`、`/menus` 之回應/狀態（授權碼 401/403）快照。
  - **Architecture（NetArchTest）**：授權相依方向（Web → UseCases，不反向）。
- **理由**：授權為高風險橫切面，以「授權矩陣」功能測試把關有/無可見性、讀寫分界、超管移除、無防鎖死；契約快照防止回應形狀漂移（Principle XV）。既有依賴超管/防鎖死之測試需同步調整。
- **替代方案**：僅單元測試授權處理器 → 無法涵蓋端到端授權矩陣與跨頁情境。否決。

未解決項目：無。釐清時延後的「端點↔選單對應機制」已於 R1 解決（route 為授權鍵 + 原則供應器）。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)：無違規（Phase 0 前與設計後複查皆 PASS）。可見選單路由查詢於 UseCases（Clean Architecture，相依向內）；不新增聚合與資料表（DDD、資料表慣例）；「取得可見選單路由」為 Query、授權判定屬 Web 橫切面（CQRS）；測試先行並含授權矩陣、契約快照、架構測試（測試層級、TDD、測試護欄）；未新增第三方套件（相依治理）；無權限回 403 ProblemDetails、授權失敗記錄不含機密（UX 一致性、Logging）。

需說明的取捨（原 Complexity Tracking，非憲章違規）：

| 取捨 | 為何需要 | 較簡單方案被否原因 |
|------|---------|-------------------|
| 移除防鎖死；種子授予 admin 初始可見性 | 使用者明確指定移除防鎖死（FR-005/008）；為開箱即用改由種子授予 admin 管理選單可見性 | 保留防鎖死＝違背使用者明示需求；完全不種可見性＝admin 登入後無選單、須人工改資料表才能起步 |
| 以選單 `Route` 為授權鍵（非新增 permission key 欄位） | 沿用既有 `SysTasks.Route`，零資料表變更、對齊「資料表有對應選單就有功能」 | 新增 permission key 欄位＝擴張結構且非必要；代價是 route 需維持標準不可任意更名（已記為約束） |
| 對稱授權（讀寫皆依可見性），僅 `GET /roles` 例外 | 對齊釐清（2026-08-31 修訂為對稱模型）：讀取亦依可見性 | 唯讀全開放＝他頁資料可被無可見性者讀取，違背對稱模型；唯一跨頁共用查詢 `GET /roles` 以 B 方案例外處理 |

**安全備註（明示取捨，非違規）**：使用者明確要求**移除防鎖死**並接受「設定不當可能導致介面無人可管理、須人工改資料表恢復」之風險（FR-005/008）；憲章無「必須防鎖死」之硬性原則，故不構成違規。

## Test Plan（測試計畫）

### 自動化測試

- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/Authorization/GetVisibleMenuRoutesTests.cs`：空角色、未知角色回空集合；多角色聯集並排除容器與未授權葉（AC-3）。
- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/Authorization/MenuAccessHandlerTests.cs`：claims 含／不含 route、B 方案 OR、無 claims、大小寫不敏感（AC-2）。
- [x] 架構測試 `backend/tests/TcmClinic.Api.UnitTests/Architecture/ArchitectureTests.cs` › `UseCasesAuth_should_not_depend_on_Infrastructure_or_Web`、`WebAuthorization_should_not_depend_on_Infrastructure`（R5 相依方向）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/MenuAuthorizationTests.cs`：授權矩陣（AC-1、AC-2、AC-3、AC-7、AC-9）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/MenuClaimsRevalidationTests.cs`：真實 Cookie 下 `menu` claims 每請求刷新（AC-10）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs`：未登入 401（AC-4）、種子 admin 開箱即用（AC-11）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/UserCrudEndpointsTests.cs`：刪除唯一管理帳號不被防鎖死攔截（AC-7）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/RoleTypeSeedTests.cs`：SUPERADMIN 種子說明不宣稱不可刪除（AC-5）。
- [x] 契約快照 `backend/tests/TcmClinic.Api.FunctionalTests/Contracts/`：授權未改回應形狀，既有快照通過。

執行（需 Docker Desktop）：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.UnitTests
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests -- -class "TcmClinic.Api.FunctionalTests.MenuAuthorizationTests"
```

### 手動走查

> 原走查以 `/admin/users`、`/admin/roles` 為授權鍵撰寫；2026-10-02 起請分別以 `accountMaintenance`、`rolePermission` 代入。初始可見性原為人工設定，後改由種子提供 admin 初始可見性（見 Clarifications）；以下測試角色 R 的可見性仍需人工設定。

1. **有可見性 → 該頁讀寫可行（US1、SC-001）**：建立角色 R，於 DB 授予 R 對 `/admin/users` 選單之可見性（`SysRoleTasks`）；建立使用者 U 指派 R 並登入。預期 `POST /users`、`PUT /users/{id}`、`DELETE /users/{id}`、`PUT /users/{id}/password` 皆成功（201/200/204/204），`GET /users` 亦 200。
2. **無可見性 → 讀寫皆被拒；B 例外（US1、SC-002、FR-006a）**：R 僅有 `/admin/users` 可見性。預期 `GET /roles/{id}/menu-visibility`、`POST/PUT/DELETE /roles` → 403（讀取亦擋）；`GET /menus` → 403；`GET /users` → 200、`POST /users` → 201；`GET /roles` → 200（B 例外）。
3. **多角色聯集（SC-005）**：U 具角色 R1（可見 `/admin/users`）與 R2（可見 `/admin/menus`）。預期可寫入使用者管理與選單管理，對 `/admin/roles` 寫入 → 403。
4. **移除超管特例（US2、SC-003）**：對原 `SUPERADMIN` 角色執行 `PUT /roles/{id}`（改名）與 `DELETE /roles/{id}`。預期皆成功（不再回「不可刪除/不可停用」）。（刪除自 SPEC-012 FR-012 起受「已指派不可刪」約束，見驗收條件表註記。）
5. **無防鎖死（US3、SC-004、FR-008）**：以 `PUT /roles/{id}/menu-visibility` 清空所有角色對 `/admin/roles` 之可見性，預期被允許；此後介面無人可管理角色，依設計以人工改 `SysRoleTasks` 恢復。另驗證 `DELETE /users/{id}` 刪除任一帳號（含最後一個管理帳號）不被防鎖死攔截。
6. **未登入 / 已登入無權限（FR-006、FR-007）**：未登入呼叫任一受控端點 → 401（前端導向 `/login`）；已登入但無任何可見性者呼叫受控端點（讀取或寫入皆同）→ 403；但 `GET /me/menus`、`GET /auth/me` → 200。

## Dependencies（相依）

- [SPEC-002 後台選單與角色權限管理](../後台選單與角色權限管理/spec.md)：提供選單項目與「角色↔選單可見性」資料（`SysRoles`／`SysTasks`／`SysRoleTasks`）。本功能**取代** SPEC-002 之 FR-014／FR-017（超級管理員保護與僅超管存取、防鎖死）。
- [SPEC-004 使用者管理與登入](../使用者管理與登入/spec.md)：提供登入／工作階段與使用者↔角色指派。本功能**取代** SPEC-004 之 FR-018（僅超管存取）、FR-019（防鎖死）、FR-022（超管解救）。
- 本功能**不改**登入驗證機制與選單可見性的資料結構，只改「以可見性作為操作授權」與「移除超管／防鎖死」。
- **後續規格修改**：[SPEC-012 角色權限維護](../../管理業務/系統管理作業/角色權限維護/spec.md) 將角色維護端點（新增、刪除、讀寫權限設定）的守門鍵由 `/admin/roles` 改為 `rolePermission`；SPEC-014 分行資料維護 新增 FR-006a 之例外 `GET /branches/options`（僅需登入）。2026-10-02 移除舊後台選單：使用者管理改由 `accountMaintenance` 授權、`GET /roles` 與 `PUT /roles/{id}` 改依 Clarifications Session 2026-10-02；資料異動紀錄、資料訪問紀錄的查詢 API 移除。
- [SPEC-009 開發模式可見全部選單](../開發模式可見全部選單/spec.md)：於 Development 將 SUPERADMIN 可見性擴為整棵選單樹全葉。

## Risks（風險）

- **無防鎖死**：設定不當可能導致介面無人可管理，須由具資料庫存取權者直接改資料表恢復（使用者明示接受之運維取捨，FR-005/008）。
- **授權鍵＝選單 `Route`／value**：管理頁的 route 不可任意更名，否則端點常數與可見性不符而授權失效；以授權矩陣功能測試偵測回歸（R1）。
- **每請求重驗成本**：可見選單於每請求聚合一次，沿用既有每請求 DB 重驗；使用者規模放大時須重新評估。

## 實作紀錄（Archive）

- **狀態**：已實作並收斂，所有任務（T001–T035）完成。
- **主要提交**：`ec8ba38` 規格與釐清 → `990141a` 技術設計 → `371d6c3` 任務產出（改為對稱模型＋`GET /roles` B 方案）→ `287a35f` 實作（移除超管／防鎖死、讀寫依可見性）→ `84551b0` 授權矩陣與測試基礎設施 → `864b4d0`、`b9bc614` 文件對齊（種子提供 admin 初始可見性）→ `40af89d` 補齊 T002／T003／T008／T026 獨立測試（2026-09-02）→ `03cf3e8` 收斂任務 T033–T035 → `59d0e97`、`a11c2d7` SUPERADMIN 種子說明（T033）→ `3e14c4b` FR-006a 註記分行選項例外、狀態對齊（T034–T035）→ `c8e5872` 移除舊後台選單（2026-10-02）。
- **實作備註（as-built）**：
  - T008 重驗刷新測試改置於 FunctionalTests（`OnValidatePrincipal` 為 Web 管線 Cookie 事件，須以真實 Cookie 的 `AuthWebApplicationFactory` 驗證）。
  - T009 測試基礎設施：`TestAuthHandler` 注入 Role + `menu` claims、`X-Test-Menus` 標頭模擬可見性、`AuthWebApplicationFactory.GrantAdminAllMenusAsync` bootstrap。
  - T013 契約快照未變（授權未改回應形狀）。
  - T026 UnitTests 專案加引用 `TcmClinic.Api.Web` + `Microsoft.AspNetCore.App` FrameworkReference 以測 Web 層授權處理器。
  - T027 前端配合：移除隱藏超管刪除鈕之 `v-if`、更新刪除失敗文案（前端現由另一團隊維護）。
- **測試數**：2026-09-01 UnitTests 29、IntegrationTests 12、FunctionalTests 40、前端 Vitest 13，建置 0 警告 0 錯誤；2026-09-02 補齊獨立測試後 UnitTests 40、FunctionalTests 42，Failed 0。
- **審查紀錄**：規格品質檢查（原 `checklists/requirements.md`）全數通過，於 2026-08-31 核准進入技術設計；備註：三項關鍵決策（可見性＝所有功能、不設防鎖死、超管降為一般角色）由使用者明確指定，「端點↔選單」對應留待技術設計。
- **原 Spec Kit 產出**：plan.md、research.md、data-model.md、quickstart.md、tasks.md、contracts/、checklists/ 已併入本檔後移除；原始檔可由 `git show c8e5872:"specs/其他/選單導向授權模型/tasks.md"` 等方式追溯。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：2026-10-01 收斂的紅燈 commit：`9477323`（T033）。初版實作沒有單獨提交紅燈 commit（`84551b0`、`40af89d` 為測試補齊，不是紅燈），無紀錄可補。
