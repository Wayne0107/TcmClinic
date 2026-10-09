# [SPEC-002] Feature: 後台選單與角色權限管理

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-002 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 選單管理 `/admin/menus`（前端 `menu-settings`）；角色權限管理 ~~`/admin/roles`~~（前端 `role-permissions`；2026-10-02 選單移除，端點改由 `rolePermission` 授權） |
| 建立日期 | 2026-08-26 |
| 最後更新 | 2026-10-04（由 Spec Kit 格式轉為 UDS SDD 格式；內容未變） |
| 原分支 | `002-admin-menu-permissions` |
| 相關規格 | ← SPEC-004 [使用者管理與登入](../使用者管理與登入/spec.md) 使用本規格角色模型；⚠️ FR-014／FR-017 被 SPEC-005 [選單導向授權模型](../選單導向授權模型/spec.md) 取代；← SPEC-007 CHB選單與角色種子 沿用本規格資料模型；← SPEC-008 [選單API欄位對齊前端](../選單API欄位對齊前端/spec.md) 修改 `GET /me/menus` 回應形狀；← SPEC-012 [角色權限維護](../../管理業務/系統管理作業/角色權限維護/spec.md) 接手角色維護（`rolePermission`）並為可見性設定加並行權杖 |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

> **狀態沿革**：Implemented（2026-08-26）；2026-09-30 修訂已完成（資料表規範對齊，見 Clarifications Session 2026-09-30；全套 182 單元 + 92 整合 + 206 功能綠）；2026-10-02 移除舊後台選單（見 Clarifications Session 2026-10-02）。
>
> ⚠️ **部分被 SPEC-005 選單導向授權模型修改**：FR-014 的「超級管理員不可刪除／停用」與 FR-017 整條已由 SPEC-005 取代（移除超管守門與防鎖死，改為依選單可見性授權）。
>
> ⚠️ **2026-10-02 舊後台選單「使用者管理」「角色權限管理」移除**（選單管理 `/admin/menus` 暫不處理）。
>
> ⚠️ **部分被 SPEC-064 對外識別碼 UUIDv7 修改（2026-10-08）**：選單與角色 API 的識別碼（路徑 `{id}`、回應 `id`、請求 `parentId`／`orderedIds`／`grantedMenuItemIds`）由整數主鍵改為 UUIDv7 字串，頂層選單的 `parentId` 以 `null` 表示（原為 0）；內部主鍵與外鍵不變（[SPEC-064](../對外識別碼UUIDv7/spec.md)）。

## Overview（概述）

提供後台的兩個設定頁面：**選單設定頁**（維護左側導覽選單，含多層父子階層、純容器父項、啟用／停用、排序）與**角色權限設定頁**（管理角色，並以選單樹為每個角色設定可見的選單頁面）。執行期使用者只看到其所屬角色被授權的選單，且被授權子項的祖先容器自動顯示。原始需求：「先幫我寫平常後台，左邊選單的設定，以及權限，到時候有兩個頁面，設定選單的頁面，以及設定角色權限的頁面，角色權限的頁面下面可以設定可以看到那些選單」。

## Motivation（動機）

- **選單維護（US1，P1）**：選單是整個後台導覽的基礎；沒有可維護的選單，後續的角色可見性設定沒有對象可設定。這是最小可行切片。
- **角色可見選單設定（US2，P2）**：這是本功能的核心權限設定；依賴 US1 已有選單、且已有角色可選。
- **使用者只看到被授權的選單（US3，P2）**：這是設定的最終成效與安全保證；依賴 US1、US2 完成。
- **角色管理（US4，P3）**：支援性功能；即使初期只有預設角色，US1–US3 仍可運作，故列為 P3。

## Clarifications（釐清紀錄）

### Session 2026-08-26

- Q: 這兩個設定頁的管理存取權由哪種機制控管？ → A: 由固定、不可刪除的「超級管理員」角色守門（防鎖死）。
- Q: 選單項目的「對應頁面／連結」如何定義？ → A: 內部前端路由字串（route），初始資料由 SQL 種入。
- Q: 父選單項目可否為「純分組容器」（無對應路由）？ → A: 可以；連結可留空，點擊僅展開／收合子項。
- Q: 子項被授權但父容器未授權時，父容器應自動顯示或保持隱藏？ → A: 自動顯示父容器以容納已授權的子項（改寫 FR-012）。
- Q: 角色是否可「停用」？ → A: 否；角色僅有 建立/編輯/軟刪除（資料表 `SysRoles` 無角色層級 IsEnabled），移除 spec 中角色「停用」字眼。

### Session 2026-09-30

背景：DATABASE-SCHEMA §5 要求參照完整性以資料庫外鍵保證、§8 要求資料表名一律單數；本功能的選單父子關聯以「0 代表頂層」表示而未建外鍵，三張表名也是複數，皆不符規範。

- Q: 選單的父子關聯如何改為由資料庫保證？ → A: 頂層改以「無父選單」表示，父選單改為指向既有選單的外鍵。新增選單時指定的父選單不存在，系統拒絕並提示（新增 FR-020）。
- Q: 新增選單與重新排序的 API，頂層要怎麼表示？ → A: **維持以 0 表示頂層**，API 契約不變，前端不需改版；後端在入口轉換。
- Q: 升級既有資料時，若有選單指向不存在的父選單怎麼處理？ → A: **升級直接中止**，不自動修改資料，由人判斷處理。
- Q: 資料表名是否改為單數？ → A: **改**：`SysRoles`→`SysRole`、`SysTasks`→`SysTask`、`SysRoleTasks`→`SysRoleTask`，既有資料保留。取代 2026-08 以使用者提供之既有 schema 表名為準的做法。`SysUserRoles` 屬「使用者管理與登入」，於該規格處理。外部可觀察行為不變。

### Session 2026-10-02

背景：CHB 選單已併入規格（見 CHB選單與角色種子），範本早期的舊後台選單（`/admin/users`、`/admin/roles`、`/admin/audit-logs`、`/admin/access-logs`）不再使用。使用者決定直接移除並修訂相關規格，不另開規格。

- Q: 舊後台選單「使用者管理」「角色權限管理」是否保留？ → A: **移除**：種子將既有資料庫中的這兩筆選單與其角色可見性軟刪除。使用者管理改由 [使用者管理與登入](../使用者管理與登入/spec.md) 所述的「帳號維護」選單授權；角色維護改由 [角色權限維護](../../管理業務/系統管理作業/角色權限維護/spec.md) 的 `rolePermission` 選單授權（含原本仍留在 `/admin/roles` 的 `PUT /roles/{id}`）。
- Q: 「選單設定頁」（`/admin/menus`）與選單維護 API 如何處理？ → A: **暫不處理**，選單與端點維持現狀。

## Requirements（需求）

### Requirement: US1 維護側邊選單（P1）

The system SHALL 讓管理者在「選單設定頁」新增、編輯、刪除、排序、啟用／停用選單項目，並將項目組成多層父子階層。

後台管理者在「選單設定頁」建立與維護左側導覽選單：新增選單項目、設定名稱／圖示／對應頁面／顯示順序，並可將項目組成父子階層（例如「系統管理」下含「使用者」「角色」）。獨立測試：僅實作此故事即可完整測試——管理者能新增、編輯、排序、啟用/停用選單項目並看到階層結構，交付「可維護的後台選單」價值。涵蓋 FR-001～FR-007、FR-018、FR-019、FR-020。

#### Scenario: AC-1 新增選單項目
- **GIVEN** 管理者在選單設定頁
- **WHEN** 新增一個選單項目並填入名稱、對應頁面與顯示順序
- **THEN** 該項目出現在選單清單的指定位置

#### Scenario: AC-2 編輯選單項目
- **GIVEN** 已存在的選單項目
- **WHEN** 管理者編輯其名稱或圖示並儲存
- **THEN** 變更立即反映於選單清單

#### Scenario: AC-3 建立子項目
- **GIVEN** 一個父選單項目
- **WHEN** 管理者在其下新增子項目
- **THEN** 子項目以階層關係顯示於父項目之下

#### Scenario: AC-4 停用選單項目
- **GIVEN** 一個啟用中的選單項目
- **WHEN** 管理者將其停用
- **THEN** 該項目不再顯示於任何使用者的側邊選單

### Requirement: US2 設定角色可見的選單（P2）

The system SHALL 讓管理者選擇一個角色，並以選單階層樹逐項勾選該角色可以看到的選單。

管理者在「角色權限設定頁」選擇一個角色，並以選單的階層樹逐項（或整層）勾選該角色「可以看到哪些選單」。獨立測試：在已有選單與角色的前提下，為某角色勾選一組可見選單並儲存，讀回設定應與所選一致。涵蓋 FR-008～FR-010。

#### Scenario: AC-5 以選單樹顯示角色可見狀態
- **GIVEN** 角色權限設定頁列出所有角色
- **WHEN** 管理者選取某角色
- **THEN** 系統以選單階層樹顯示目前該角色的可見／不可見狀態

#### Scenario: AC-6 儲存角色可見選單
- **GIVEN** 已選取的角色
- **WHEN** 管理者勾選數個選單項目並儲存
- **THEN** 系統保存該角色的可見選單，重新進入時狀態一致

#### Scenario: AC-7 父選單含子項套用（後續增強，SHOULD）
- **GIVEN** 管理者勾選一個父選單
- **WHEN** 選擇「含子項」套用
- **THEN** 其下所有子項一併被授權為可見。（本次僅實作逐項勾選，見 FR-010。）

### Requirement: US3 使用者只看到被授權的選單（P2）

The system SHALL 讓使用者的側邊選單只顯示其所屬角色被授權可見的選單項目，未授權者完全不出現。

一般使用者登入後台後，左側選單只顯示其所屬角色被授權可見的選單項目；未授權的項目完全不出現。獨立測試：以隸屬特定角色的使用者身分檢視後台，側邊選單應恰好等於該角色被授權的可見選單集合。涵蓋 FR-011～FR-013、FR-015。

#### Scenario: AC-8 未授權項目不顯示
- **GIVEN** 某角色未被授權某選單項目
- **WHEN** 該角色的使用者檢視後台
- **THEN** 該選單項目不顯示

#### Scenario: AC-9 多角色聯集
- **GIVEN** 使用者同時擁有多個角色
- **WHEN** 檢視側邊選單
- **THEN** 顯示的項目為所有角色可見選單的聯集

#### Scenario: AC-10 變更於下次載入生效
- **GIVEN** 管理者變更了某角色的可見選單
- **WHEN** 該角色使用者於下次載入頁面
- **THEN** 側邊選單反映最新的授權結果

### Requirement: US4 管理角色（P3）

The system SHALL 讓管理者在「角色權限設定頁」建立、重新命名或刪除角色。

管理者在「角色權限設定頁」建立、重新命名或刪除角色，以便為不同職務指派不同的選單可見範圍。獨立測試：新增一個角色後，其應出現在角色清單且可被指派可見選單。涵蓋 FR-014（FR-014a 範圍外）。

#### Scenario: AC-11 新增角色
- **GIVEN** 角色權限設定頁
- **WHEN** 管理者新增一個角色並命名
- **THEN** 該角色出現在角色清單

#### Scenario: AC-12 超級管理員角色受保護
- **GIVEN** 受保護的「超級管理員」角色
- **WHEN** 管理者嘗試刪除或停用
- **THEN** 系統阻擋並提示該角色不可刪除（防鎖死）

> ⚠️ AC-12 的「超管保護／防鎖死」已由 SPEC-005 選單導向授權模型取代（FR-005：不實作防鎖死）；現行「刪除 SUPERADMIN 回 400」的原因為 SPEC-012 角色權限維護 FR-012「已指派不可刪」。

### 功能需求條文（Functional Requirements）

**選單設定頁（US1）**

- **FR-001**: 系統 MUST 允許管理者新增選單項目，並設定名稱、對應前端路由（內部 route 字串；純容器父項可留空，見 FR-019）、圖示與顯示順序。
- **FR-002**: 系統 MUST 支援選單項目的父子階層（多層巢狀）。
- **FR-003**: 管理者 MUST 能編輯既有選單項目的屬性。
- **FR-004**: 管理者 MUST 能刪除（軟刪除）選單項目。若該項目仍有未刪除的子項，系統 MUST 阻擋刪除並提示需先移除或改掛子項（不連動刪除，避免誤刪整棵子樹）。
- **FR-005**: 管理者 MUST 能調整選單項目的顯示順序。
- **FR-006**: 管理者 MUST 能啟用／停用選單項目；停用的項目 MUST NOT 顯示於任何使用者的側邊選單。
- **FR-007**: 選單名稱在同一父層級之下 MUST 唯一。
- **FR-018**: 系統 MUST 阻擋會造成循環的階層設定。
- **FR-020**: 新增選單項目時若指定的父選單不存在，系統 MUST 拒絕並提示父選單不存在；父子關聯 MUST 由資料庫參照完整性保證（2026-09-30 新增）。
- **FR-019**: 選單項目的「對應前端路由」對純容器父項 MAY 為空；此類父項點擊時僅展開／收合子項，不進行導覽。

**角色權限設定頁（US2、US4）**

- **FR-008**: 系統 MUST 提供角色清單，顯示所有可設定的角色。
- **FR-009**: 管理者 MUST 能為指定角色逐項設定其可見的選單項目。
- **FR-010**: 設定角色可見性時，系統 MUST 以選單階層樹呈現並支援**逐項勾選**。整層（含子項）批次套用列為 **SHOULD**（後續增強）。
- **FR-014**: 管理者 MUST 能建立、編輯與刪除（軟刪除）角色。「超級管理員」角色 MUST NOT 可被刪除或停用。
  - > ⚠️ **「超級管理員不可刪除/停用」已由 SPEC-005（選單導向授權）取代**：SPEC-005 移除超管特例，角色一律可編輯/刪除。見 [選單導向授權模型](../選單導向授權模型/spec.md)。「可建立/編輯/刪除角色」本身仍有效。
- **FR-014a（範圍外，待帳號系統整合）**: 刪除仍有使用者指派的角色時，系統 SHOULD 阻擋並提示須先解除指派。因使用者↔角色指派屬既有帳號系統（見假設），本次**不實作**此檢核。

**權限套用與強制（US3）**

- **FR-011**: 未授權給某角色的選單項目（實際頁面），該角色使用者 MUST NOT 看見（預設拒絕）。
- **FR-012**: 選單可見性以「具路由的子項（實際頁面）」為授權單位。當某角色被授權看到一個子項時，其所有祖先父容器 MUST 自動顯示以容納該子項，即使父容器未被個別授權；父容器本身不需獨立授權。
- **FR-013**: 角色可見性變更後，系統 MUST 於使用者下次載入頁面時套用最新結果。
- **FR-015**: 一位使用者可擁有多個角色；其可見選單 MUST 為所有所屬角色可見選單的聯集。

**共通**

- **FR-016**: 系統 MUST 透過每列的稽核欄位（`CreateUser/CreateDate/ModifyUser/ModifyDate`）記錄選單與角色可見性的最後變更者與時間；授權的撤銷以軟刪除（`Removed=1`）保留紀錄。完整逐次變更歷史（audit log）不在本次範圍。
- **FR-017**: 這兩個設定頁 MUST 僅限「超級管理員」角色存取；該角色 MUST 永遠可存取本功能，且 MUST NOT 可被刪除或停用（避免鎖死）。
  - > ⚠️ **已由 SPEC-005（選單導向授權）整條取代**：改為「對該頁選單有可見性者即可存取其寫入功能」、移除超管守門與防鎖死。見 [選單導向授權模型](../選單導向授權模型/spec.md)。

### 邊界情境（Edge Cases）

- 停用一個已被某角色授權的選單項目時，該項目對所有使用者一律不顯示（停用優先於授權）。
- 刪除一個已被授權的選單項目時，該項目自動從所有角色的可見性設定中移除。
- 建立父子階層時若形成循環（A 為 B 的子、B 又為 A 的子），系統必須阻擋。
- 刪除仍有未刪除子項的父選單時，系統阻擋刪除並提示需先處理子項（FR-004）。
- 某角色沒有任何可見選單時，該角色使用者的側邊選單為空並顯示適當提示。
- 使用者未隸屬任何角色時，看不到任何需授權的選單項目（預設拒絕）。
- 父容器未被個別授權、但其下有被授權的子項時，父容器自動顯示以容納子項（見 FR-012）。

### 關鍵實體（Key Entities）

- **選單項目（Menu Item）**：後台側邊導覽的一個節點。屬性含名稱、圖示、對應前端路由（內部 route 字串，純容器父項可為空）、顯示順序、啟用狀態；透過父項參考形成階層樹。
- **角色（Role）**：一組職務權限的集合。屬性含名稱、代碼（RoleCode）、描述；**無啟用/停用狀態，僅軟刪除（Removed）**；與可見選單為多對多關係。其中包含一個受保護的「超級管理員」角色，不可刪除，永遠可存取本功能兩個設定頁（⚠️ 超管保護已由 SPEC-005 取代）。
- **角色選單可見性（Role Menu Visibility）**：角色與選單項目之間的可見對應（哪個角色可看到哪些選單）。
- **使用者（User）**：後台操作者，關聯一或多個角色（指派機制屬既有帳號系統，非本功能範圍）；其可見選單由所屬角色決定。

### 假設（Assumptions）

- 規格內容依憲章 Principle VIII 以繁體中文撰寫；`FR-###`／`SC-###`／`P1` 等標記保留原樣，作為下游追溯的結構標記。
- 本功能所稱「權限」僅指「選單可見性」；頁面內按鈕／動作層級（檢視／新增／編輯／刪除）的細粒度權限不在本次範圍。
- 角色管理（建立／編輯／刪除）納入「角色權限設定頁」，與可見性設定同頁操作。
- 使用者與角色的指派由既有帳號管理機制負責，不在本功能範圍；本功能假設可讀取角色清單及使用者的所屬角色。
- 選單支援多層父子巢狀；權限採預設拒絕（未授權即不可見）。
- 選單「對應連結」為內部前端路由字串（前端 route）；初始選單／角色資料可由 SQL 種入。
- 這兩個設定頁的存取控制由固定、不可刪除的「超級管理員」角色負責。（⚠️ 已由 SPEC-005 取代）
- 停用狀態優先於授權：被停用的選單即使已授權給角色，也一律不顯示。

## Acceptance Criteria（驗收條件）

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、FR-001 | 新增選單後出現在選單樹 | `backend/tests/TcmClinic.Api.FunctionalTests/MenuEndpointsTests.cs` › `Create_then_appears_in_tree` |
| AC-2 | US1 情境 2、FR-003 | 編輯名稱／圖示後立即反映 | `backend/tests/TcmClinic.Api.UnitTests/MenuAggregate/MenuItemTests.cs` › `Mutation_methods_are_fluent_and_apply`；端到端編輯：手動：Test Plan 手動走查第 3 步 |
| AC-3 | US1 情境 3、FR-002 | 子項以階層顯示於父項之下 | `MenuEndpointsTests` › `Create_child_under_existing_parent`；`backend/tests/TcmClinic.Api.UnitTests/MenuAggregate/MenuTreeTests.cs` › `Build_nests_children_under_parents_ordered` |
| AC-4 | US1 情境 4、FR-006 | 停用項目即使被授權也不顯示 | `backend/tests/TcmClinic.Api.UnitTests/MenuAggregate/MenuVisibilityTests.cs` › `Disabled_item_is_excluded_even_if_granted` |
| AC-5 | US2 情境 1、FR-008、FR-010 | 選角色後以選單樹顯示可見狀態；角色清單可讀 | `backend/tests/TcmClinic.Api.FunctionalTests/RoleVisibilityEndpointsTests.cs` › `Get_roles_includes_seeded_superadmin`、`Get_visibility_for_missing_role_returns_404` |
| AC-6 | US2 情境 2、FR-009 | 勾選儲存後讀回一致 | `RoleVisibilityEndpointsTests` › `Grant_leaf_is_reflected_in_visibility` |
| AC-7 | US2 情境 3、FR-010（SHOULD） | 「含子項」整層套用 | 手動：Code Review 確認本次僅實作逐項勾選（後續增強，未實作） |
| AC-8 | US3 情境 1、SC-004、FR-011、FR-012 | 未授權項不顯示，祖先容器自動顯示（未授權可見率 0%） | `MenuVisibilityTests` › `Ungranted_leaves_are_hidden`、`Granted_leaf_shows_with_ancestor_container`；`backend/tests/TcmClinic.Api.FunctionalTests/NavigationEndpointsTests.cs` › `My_menu_shows_granted_leaf_with_ancestor_and_hides_others` |
| AC-9 | US3 情境 2、FR-015 | 多角色時可見選單為聯集 | `MenuVisibilityTests` › `Multiple_grants_are_union`；`backend/tests/TcmClinic.Api.UnitTests/Authorization/GetVisibleMenuRoutesTests.cs` › `Union_excludes_container_and_ungranted` |
| AC-10 | US3 情境 3、SC-003、FR-013 | 可見性變更後下次載入即反映 | `NavigationEndpointsTests` › `My_menu_shows_granted_leaf_with_ancestor_and_hides_others`；`backend/tests/TcmClinic.Api.FunctionalTests/MenuClaimsRevalidationTests.cs` › `Visibility_change_reflected_in_menu_claims_next_request` |
| AC-11 | US4 情境 1、FR-014 | 新增角色後出現在清單 | `backend/tests/TcmClinic.Api.FunctionalTests/RoleCrudEndpointsTests.cs` › `Create_role_then_appears_in_list`、`Duplicate_code_returns_400`、`Update_role_changes_name`、`Delete_role_removes_it` |
| AC-12 | US4 情境 2、FR-014、FR-017 | 刪除 SUPERADMIN 被阻擋 | `RoleCrudEndpointsTests` › `Delete_assigned_superadmin_returns_400` |
| AC-13 | SC-001 | 管理者 2 分鐘內完成新增一個選單項目並指定階層位置與順序 | 手動：Test Plan 手動走查第 3、4 步計時 |
| AC-14 | SC-002 | 管理者 3 分鐘內完成為一個角色設定可見選單 | 手動：Test Plan 手動走查第 6 步計時 |
| AC-15 | SC-005 | 90% 管理者首次即正確完成角色可見性設定 | 手動：使用者可用性觀察（Test Plan 手動走查第 6、7 步） |
| AC-16 | FR-007 | 同層名稱唯一（含頂層） | `backend/tests/TcmClinic.Api.UnitTests/MenuAggregate/MenuHandlerTests.cs` › `Create_rejects_duplicate_name_in_same_level`；`MenuEndpointsTests` › `Duplicate_name_in_same_level_returns_400`；`backend/tests/TcmClinic.Api.IntegrationTests/Data/MenuRoleSchemaTests.cs` › `Top_level_menu_names_are_unique` |
| AC-17 | FR-018 | 造成循環的階層設定被阻擋 | `MenuTreeTests` › `WouldCreateCycle_detects_self_and_descendant_parenting` |
| AC-18 | FR-020 | 父選單不存在時拒絕（400），父子關聯由資料庫外鍵保證 | `MenuHandlerTests` › `Create_rejects_missing_parent`；`MenuEndpointsTests` › `Create_with_missing_parent_returns_400`；`MenuRoleSchemaTests` › `SysTask_parent_is_nullable_self_reference` |
| AC-19 | FR-004 | 刪除仍有子項的父選單被阻擋 | `MenuHandlerTests` › `Delete_blocks_when_item_has_children`、`Delete_returns_NotFound_when_missing` |
| AC-20 | FR-012（授權單位為子項）、FR-019 | 容器不可被授權（400）；無路由項為純容器 | `RoleVisibilityEndpointsTests` › `Granting_a_container_returns_400`；`MenuItemTests` › `Item_without_route_is_container`、`Blank_route_is_normalized_to_container` |
| AC-21 | Clarifications 2026-09-30、設計決策 R8 | API 以 `parentId = 0` 表示頂層、資料庫存 `NULL`；表名改單數 | `MenuHandlerTests` › `Create_with_zero_parent_creates_top_level_item`；`MenuItemTests` › `MoveTo_null_moves_item_to_top_level`；`MenuRoleSchemaTests` › `System_tables_use_singular_names`、`Join_tables_reference_singular_tables` |
| AC-22 | Clarifications 2026-09-30、設計決策 R8 | 升級時 0 轉 `NULL`；有孤兒父選單時 migration 中止 | `backend/tests/TcmClinic.Api.IntegrationTests/Data/MenuParentFkMigrationTests.cs` › `Zero_parent_becomes_null_and_children_are_kept`、`Orphan_parent_aborts_migration` |
| AC-23 | Clarifications 2026-10-02 | 舊後台選單「使用者管理」「角色權限管理」與其可見性由種子軟刪除，`/admin/menus` 不受影響 | `backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbMenuTreeSeedTests.cs` › `Reseeding_removes_retired_admin_menus` |
| AC-24 | FR-016 | 稽核欄位自動填入、刪除轉軟刪除 | 手動：Code Review 確認 `AuditableEntityInterceptor` 自動填 Create/Modify Date/User 並將刪除轉為 `Removed=1`（設計決策 R6） |

> AC-12：超管保護已由 SPEC-005 取代；現行測試驗證的是 SPEC-012 FR-012「已指派角色不可刪」，`Delete_assigned_superadmin_returns_400` 因初始 admin 使用者已指派 SUPERADMIN 而回 400。
>
> AC-5、AC-6：自 SPEC-012 起，設定可見性需帶並行權杖（`rowVersion`，取自 GET menu-visibility 的 ETag），測試已隨之更新。
>
> AC-8、AC-10：`GET /me/menus` 回應形狀自 SPEC-008 起改為 `text`／`value`／`subMenu`，測試以新形狀驗證。

## Technical Design（技術設計）

### 技術脈絡

- 語言／版本：C# / .NET 10（後端，global.json 釘選 10.0.201）；TypeScript（前端，Vue 3）。
- 主要相依：FastEndpoints、EF Core、Mediator（source generator）、Ardalis.Specification / Result / GuardClauses / SmartEnum、Vogen、**TreeCollections**（選單樹）、Serilog + OpenTelemetry；前端 Vue 3 + Pinia + Vue Router + Vite。
- 儲存：SQL Server（正式與開發一致）；開發以 .NET Aspire 佈建 SQL Server 容器、測試以 Testcontainers（SQL Server 2022）；EF Core migrations；資料表 `SysRole`／`SysTask`／`SysRoleTask`（2026-09-30 前為複數，軟刪除 `Removed`）。初始選單與角色資料由 SQL 種入。
- 測試：xUnit v3、NSubstitute、Testcontainers（SQL Server 2022）、CsCheck（property-based）、Verify.XunitV3（OpenAPI/DTO 契約 snapshot）、NetArchTest.Rules（分層）、Stryker.NET（變異）；前端 Vitest + Vue Test Utils，E2E 用 Playwright。
- 目標平台：Web（.NET 後端 API + 瀏覽器前端），.NET Aspire 編排；monorepo（`apphost/` + `backend/` + `frontend/`）。
- 效能目標（Principle XIV）：讀取端點 ≤ 300 ms p95、寫入端點 ≤ 500 ms p95；選單樹查詢避免 N+1。
- 限制：權限預設拒絕；超級管理員角色（`RoleCode='SUPERADMIN'`）不可鎖死/刪除（⚠️ 已由 SPEC-005 取代）；稽核欄位 + `Removed` 軟刪除 + 全域查詢過濾器（Principle XI）；選單連結為內部前端路由字串；停用優先於授權。
- 規模：後台管理規模——選單項目數十筆、角色數十個、使用者數百至數千；2 個設定頁 + 1 個執行期選單查詢。
- 技術取向依憲章：Clean Architecture 四層 + DDD、FastEndpoints（REPR）、EF Core、Mediator、Vogen 強型別 ID、選單階層以 TreeCollections 建模、前端 Vue 3 功能導向組織。本功能為 greenfield，第一步先 scaffold。
- 資料模型以使用者提供的現成 schema（`SysRoles`/`SysTasks`/`SysRoleTasks`）為基礎、依憲章調整後定案：主鍵改用 Vogen client-side ValueGenerator（非 IDENTITY）、時間欄用 `datetime`、超級管理員以固定 `RoleCode='SUPERADMIN'` 辨識、三表皆含 `Removed` 軟刪除。

### 受影響元件

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.Core/MenuAggregate/MenuItem.cs`、`MenuItemId.cs`、`MenuItemName.cs` | 選單聚合：名稱／路由／圖示／順序／啟用／父項；變更方法回傳 `this`；`ParentId` 為 `MenuItemId?`（`null`＝頂層） |
| `backend/src/TcmClinic.Api.Core/MenuAggregate/MenuTree.cs` | 建樹與循環偵測純函式（FR-018） |
| `backend/src/TcmClinic.Api.Core/MenuAggregate/Specifications/MenuItemSpecs.cs` | 依父層列出、建樹用全列等 Specifications（`TagWith`） |
| `backend/src/TcmClinic.Api.Core/RoleAggregate/Role.cs`、`RoleId.cs`、`RoleCode.cs`、`RoleName.cs`、`RoleMenuVisibility.cs`、`RoleMenuVisibilityId.cs` | 角色聚合與角色可見選單實體 |
| `backend/src/TcmClinic.Api.Core/RoleAggregate/Specifications/RoleSpecs.cs` | 角色可見性 Specifications |
| `backend/src/TcmClinic.Api.UseCases/Menus/`（`CreateMenuItem.cs`、`UpdateMenuItem.cs`、`DeleteMenuItem.cs`、`ReorderMenuItems.cs`、`SetMenuItemEnabled.cs`、`GetMenuTree.cs`、`MenuParent.cs`、`MenuTreeDto.cs`） | 選單命令與查詢；入口將 `parentId = 0` 轉為 `null`，建立前驗證父選單存在（FR-020） |
| `backend/src/TcmClinic.Api.UseCases/Roles/`（`GetRoles.cs`、`CreateRole.cs`、`UpdateRole.cs`、`DeleteRole.cs`、`GetRoleMenuVisibility.cs`、`SetRoleMenuVisibility.cs`） | 角色 CRUD 與可見選單讀寫（拒絕容器 id） |
| `backend/src/TcmClinic.Api.UseCases/Navigation/GetMyMenu.cs`、`MyMenuNodeDto.cs` | 執行期依角色解析可見選單樹 |
| `backend/src/TcmClinic.Api.Web/Endpoints/Menus/`（Create／Update／Delete／Reorder／SetMenuEnabled／GetMenuTree 端點） | 選單 REPR 端點 |
| `backend/src/TcmClinic.Api.Web/Endpoints/Roles/`（GetRoles／CreateRole／UpdateRole／DeleteRole／GetRoleVisibility／SetRoleVisibility 端點） | 角色與可見選單端點 |
| `backend/src/TcmClinic.Api.Web/Endpoints/Navigation/GetMyMenuEndpoint.cs` | `GET /me/menus` |
| `backend/src/TcmClinic.Api.Web/Configurations/AuthConfigs.cs` | 端點授權接線（原超級管理員原則，後由 SPEC-005 改為選單可見性授權） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Config/RoleConfig.cs`、`MenuItemConfig.cs`、`RoleMenuVisibilityConfig.cs` | EF 設定：`ToTable("SysRole"/"SysTask"/"SysRoleTask")`、Vogen 轉換、篩選唯一索引、`ParentId` 自參考外鍵（Restrict） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs`、`backend/src/TcmClinic.Api.SharedKernel/AuditableEntityBase.cs` | 稽核欄位自動填入、刪除轉軟刪除（FR-016） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/SeedData.cs` | 種入 SUPERADMIN 角色與初始選單；2026-10-02 起軟刪除舊後台選單、只再種入 `/admin/menus` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20261001035329_SingularSysTablesAndMenuParentFk.cs` | 2026-09-30 修訂 migration：更名單數表名、`ParentId` 改可空外鍵、孤兒中止 |
| `backend/stryker-config.json` | 變異測試設定（保留，見實作備註） |
| `frontend/src/shared/components/TreeView.vue`、`CheckableTreeView.vue`、`AppSidebar.vue`、`SidebarTree.vue`、`tree.types.ts`、`nav.types.ts` | 共用樹狀元件與側邊選單 |
| `frontend/src/features/menu-settings/` | 選單設定頁（page/api/store/routes/types + `menu.store.spec.ts`） |
| `frontend/src/features/role-permissions/` | 角色權限頁（角色清單 + 選單樹勾選 + `role.store.spec.ts`） |
| `THIRD-PARTY-NOTICES.md` | 登記 TreeCollections、Serilog.Sinks.Console、Aspire.Hosting.SqlServer 等新相依 |

### 資料模型

以使用者提供的現成資料庫（DataBase.md）為基礎，依憲章調整後定案。所有實體遵循 Principle VII（Vogen 強型別 ID，client-side `ValueGenerator`，**非 IDENTITY**）與 Principle XI（稽核欄位 `CreateDate/CreateUser/ModifyDate/ModifyUser`；時間用 `datetime`、本地時間；由 `AuditableEntityBase` + `SaveChangesInterceptor` 自動填入）。三張表皆有 `Removed` bit 軟刪除。

> **2026-09-30 修訂（設計決策 R8；Clarifications Session 2026-09-30）**：
> - 表名改為單數（DATABASE-SCHEMA §8）：`SysRoles`→`SysRole`、`SysTasks`→`SysTask`、`SysRoleTasks`→`SysRoleTask`，以 `RenameTable` 保留資料；主鍵、外鍵、索引名隨表名改為 EF 預設命名。下文表名已更新，對照表見本節末。
> - `SysTask.ParentId` 改為可空的自參考外鍵（DATABASE-SCHEMA §5）：`NULL`＝頂層，取代原「0＝頂層」哨兵。

#### Role（角色）— 表 `SysRole`（原 `SysRoles`）

| 欄位 | 型別 | 領域對應 / 規則 |
|---|---|---|
| Id | int（Vogen `RoleId`） | 主鍵，非 IDENTITY |
| Code | nvarchar(50) | 領域值物件 `RoleCode`；角色代碼，未刪除者唯一；**超級管理員固定 `SUPERADMIN`**，不可刪除（取代 IsSystem 旗標，R3/FR-017） |
| Name | nvarchar(20) | 領域值物件 `RoleName`；角色名稱 |
| Description | nvarchar(200), null | 說明 |
| Removed | bit | 軟刪除（角色「刪除」＝ `Removed=1`） |

**不變量 / 行為**：`Code='SUPERADMIN'` 的角色不可刪除（FR-017；⚠️ 已由 SPEC-005 取代）；刪除仍有使用者指派的角色時阻擋屬**範圍外、本次不實作**（FR-014a，待帳號系統整合）。

#### MenuItem（選單項目）— 表 `SysTask`（原 `SysTasks`）

| 欄位 | 型別 | 領域對應 / 規則 |
|---|---|---|
| Id | int（Vogen `MenuItemId`） | 主鍵，非 IDENTITY |
| ParentId | int, null（Vogen `MenuItemId?`） | **NULL = 頂層**；否則為自參考外鍵 → `SysTask.Id`，`OnDelete: Restrict`。形成階層樹（2026-09-30 前為 NOT NULL、0 = 頂層、無外鍵） |
| Route | nvarchar(200), null | 前端路由字串；純容器父項為 NULL（FR-019、釐清 Q2） |
| Name | nvarchar(30) | 領域值物件 `MenuItemName`；選單名稱；同一 `ParentId` 下唯一（FR-007） |
| Icon | nvarchar(50), null | 圖示 |
| DisplayOrder | int | 同層顯示順序（FR-005） |
| IsEnabled | bit | 停用不顯示於任何使用者（FR-006，停用優先於授權） |
| Removed | bit | 軟刪除 |

**不變量 / 行為**：同層名稱唯一（FR-007，唯一索引 `(ParentId, Name)` 篩選 `Removed = 0` + 領域檢查；SQL Server 唯一索引將多個 NULL 視為相同值，故頂層名稱同樣唯一）；`ParentId` 變更不得造成循環（FR-018，以走訪偵測）；父選單必須存在（FR-020，用例層先驗證回 400，資料庫外鍵兜底）；刪除父項時處理子項（FR-004；軟刪除不觸發外鍵，`Restrict` 僅擋實體刪除）；API 層仍以 `parentId = 0` 表示頂層，用例入口轉為 `null`（契約不變）；變更方法回傳 `this`（Principle II）。選單樹以 TreeCollections（`MutableEntityTreeNode<…, MenuItemId, …>`）建模，載入後用 `AutoBuiltTreeSource` 由扁平列建樹。

#### RoleMenuVisibility（角色可見選單）— 表 `SysRoleTask`（原 `SysRoleTasks`）

| 欄位 | 型別 | 領域對應 / 規則 |
|---|---|---|
| Id | int（Vogen） | 代理主鍵，非 IDENTITY |
| SysRoleId | int（`RoleId`） | FK → SysRole，ON DELETE CASCADE |
| SysTaskId | int（`MenuItemId`） | FK → SysTask，ON DELETE CASCADE；僅授權「具 Route 的子項」 |
| Removed | bit | 軟刪除 |

**規則**：`(SysRoleId, SysTaskId)` 未刪除者唯一；只記錄被明確授權的「有路由子項」，容器不入表（執行期自動回溯顯示，FR-012）；預設拒絕：不在表中即不可見（FR-011）。

> 註：SPEC-007 起容器改以「是否有子節點」判定（容器的 `Route` 亦存 `value`），2026-09-30 修訂後 `SetRoleMenuVisibility` 的容器判定見設計決策 R8 與 SPEC-007。

#### User（使用者，本功能範圍外，僅參考）

- 使用者與角色的指派由既有帳號系統維護；本功能只讀取使用者的所屬角色（FR-015 聯集）。

#### 選單可見性解析（執行期，`GET /me/menus`）

輸入：使用者所屬、未刪除（`Removed=0`）的角色集合。

1. 取這些角色在 `SysRoleTask`（未刪除）的 `SysTaskId` 聯集 → 被授權的「子項」集合。
2. 以 TreeCollections 由這些子項向上回溯，補入其所有祖先容器（FR-012 自動顯示）。
3. 過濾掉 `IsEnabled=0` 或 `Removed=1` 的節點（停用/刪除優先，FR-006）。
4. 依 `DisplayOrder` 排序後輸出選單樹。

結果：可見選單 = 授權子項 + 其祖先容器，去除停用/刪除者。

#### 種子資料（SQL）

- 一列 `Code='SUPERADMIN'` 的超級管理員角色（不可刪除，FR-017）。
- 初始 `SysTask`（選單）與 `SysRoleTask`（授權），由使用者提供的 SQL 種入。
- 2026-10-02 起：種子將舊後台選單「使用者管理」（`/admin/users`）、「角色權限管理」（`/admin/roles`）與其角色可見性軟刪除，只再種入 `/admin/menus`（Clarifications Session 2026-10-02）。

#### 與領域命名的對照（EF 設定層處理）

| 資料表 | 原表名（2026-09-30 前） | 領域聚合 |
|---|---|---|
| SysRole | SysRoles | Role |
| SysTask | SysTasks | MenuItem |
| SysRoleTask | SysRoleTasks | RoleMenuVisibility |

實體名稱（MenuItem/Role/RoleMenuVisibility）於 EF `IEntityTypeConfiguration` 對應到上述資料表名。

#### 資料庫綱要（原 contracts/database-schema.sql，權威 SQL，as-built）

```sql
-- 後台選單與角色權限管理 — 資料庫綱要（憲章合規版；as-built 對齊；2026-09-30 表名改單數、ParentId 改自參考外鍵，research R8）
-- 來源：使用者提供的 DataBase.md 草稿，依憲章調整：
--   * 主鍵移除 IDENTITY，改由 Vogen client-side ValueGenerator 指派（Principle VII）
--   * CreateDate/ModifyDate 由 datetime2 改為 datetime、存本地時間（Principle XI）
--   * 選單目標由 Area/Controller/Action（MVC）改為單一前端路由 Route（Vue SPA；澄清 Q2）
--
-- 欄名對齊實作（as-built）：EF 未設 HasColumnName，欄名直接採領域屬性名，故：
--   * SysRole：Code（原草稿 RoleCode）、Name（原草稿 RoleName）
--   * SysTask：Name（原草稿 TaskName）、Icon（原草稿 IconName）、DisplayOrder（原草稿 TaskSort）
--   * 一致者：ParentId、Route、IsEnabled、SysRoleId、SysTaskId、稽核欄位、Removed
-- 若要對接你「現成資料庫」的原始欄名（RoleCode/RoleName/TaskName/IconName/TaskSort），
-- 請改在 EF 設定（RoleConfig/MenuItemConfig）為對應屬性加上 .HasColumnName("RoleCode") 等，
-- 並重新產生 migration；本契約以目前實作為單一事實來源。

USE [ChbApi]
GO

-- 角色（對應領域 Role 聚合）
CREATE TABLE [dbo].[SysRole](
    [Id]          [int]           NOT NULL,   -- Vogen RoleId；非 IDENTITY，由 ValueGenerator 指派
    [Code]        [nvarchar](50)  NOT NULL,   -- 角色代碼；超級管理員固定為 'SUPERADMIN'（不可刪/改）
    [Name]        [nvarchar](20)  NOT NULL,
    [Description] [nvarchar](200) NULL,
    [CreateDate]  [datetime]      NOT NULL,   -- 本地時間（Principle XI）
    [CreateUser]  [nvarchar](50)  NOT NULL,
    [ModifyDate]  [datetime]      NOT NULL,
    [ModifyUser]  [nvarchar](50)  NOT NULL,
    [Removed]     [bit]           NOT NULL CONSTRAINT [DF_SysRole_Removed] DEFAULT (0),  -- 軟刪除
    CONSTRAINT [PK_SysRole] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO
CREATE UNIQUE INDEX [UX_SysRole_Code] ON [dbo].[SysRole]([Code]) WHERE [Removed] = 0
GO

-- 選單項目（對應領域 MenuItem 聚合；自參考階層）
CREATE TABLE [dbo].[SysTask](
    [Id]           [int]           NOT NULL,    -- Vogen MenuItemId；非 IDENTITY
    [ParentId]     [int]           NULL,        -- 父選單；NULL = 頂層（root）；自參考外鍵（2026-09-30 修訂，原 NOT NULL DEFAULT 0）
    [Route]        [nvarchar](200) NULL,        -- 前端路由字串；純容器父項為 NULL（FR-019）
    [Name]         [nvarchar](30)  NOT NULL,    -- 選單名稱（同一 ParentId 下唯一，FR-007）
    [Icon]         [nvarchar](50)  NULL,
    [DisplayOrder] [int]           NOT NULL,    -- 同層顯示順序（FR-005）
    [IsEnabled]    [bit]           NOT NULL CONSTRAINT [DF_SysTask_IsEnabled] DEFAULT (1),  -- 停用不顯示（FR-006）
    [CreateDate]   [datetime]      NOT NULL,
    [CreateUser]   [nvarchar](50)  NOT NULL,
    [ModifyDate]   [datetime]      NOT NULL,
    [ModifyUser]   [nvarchar](50)  NOT NULL,
    [Removed]      [bit]           NOT NULL CONSTRAINT [DF_SysTask_Removed] DEFAULT (0),
    CONSTRAINT [PK_SysTask] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO
-- 註：ParentId 為自參考外鍵（NULL＝頂層；2026-09-30 修訂，原採 0=root 慣例、無外鍵）；循環阻擋（FR-018）與同層唯一（FR-007）另由領域層強制。
ALTER TABLE [dbo].[SysTask]
    ADD CONSTRAINT [FK_SysTask_SysTask_ParentId] FOREIGN KEY([ParentId])
    REFERENCES [dbo].[SysTask]([Id])
GO
CREATE UNIQUE INDEX [UX_SysTask_Parent_Name] ON [dbo].[SysTask]([ParentId], [Name]) WHERE [Removed] = 0
GO

-- 角色可見選單（對應領域 RoleMenuVisibility；角色↔具路由的選單子項）
CREATE TABLE [dbo].[SysRoleTask](
    [Id]         [int]      NOT NULL,          -- Vogen 代理鍵；非 IDENTITY
    [SysRoleId]  [int]      NOT NULL,
    [SysTaskId]  [int]      NOT NULL,
    [CreateDate] [datetime] NOT NULL,
    [CreateUser] [nvarchar](50) NOT NULL,
    [ModifyDate] [datetime] NOT NULL,
    [ModifyUser] [nvarchar](50) NOT NULL,
    [Removed]    [bit]      NOT NULL CONSTRAINT [DF_SysRoleTask_Removed] DEFAULT (0),
    CONSTRAINT [PK_SysRoleTask] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO
ALTER TABLE [dbo].[SysRoleTask]
    ADD CONSTRAINT [FK_SysRoleTask_SysRole] FOREIGN KEY([SysRoleId])
    REFERENCES [dbo].[SysRole]([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SysRoleTask]
    ADD CONSTRAINT [FK_SysRoleTask_SysTask] FOREIGN KEY([SysTaskId])
    REFERENCES [dbo].[SysTask]([Id]) ON DELETE CASCADE
GO
CREATE UNIQUE INDEX [UX_SysRoleTask_Role_Task] ON [dbo].[SysRoleTask]([SysRoleId], [SysTaskId]) WHERE [Removed] = 0
GO

-- 種子：超級管理員角色（Code='SUPERADMIN'，不可刪除/停用；FR-017、R3）
-- 及初始選單與 SysRoleTask 授權，由使用者提供的 SQL 種入。
```

> 上列 SQL 為綱要說明；實際索引名依 EF 預設命名（如 `IX_SysTask_ParentId_Name`，見設計決策 R8 與整合測試 `MenuRoleSchemaTests`）。

### API 契約

> ⚠️ **授權已由 SPEC-005 取代**：改為依選單可見性授權（無可見性回 403）。2026-10-02 起守門鍵：選單管理端點維持 `/admin/menus`；`GET /roles` 為 `accountMaintenance` **或** `rolePermission` 任一可見；`PUT /roles/{id}` 改 `rolePermission`；角色新增、刪除與可見性讀寫由 SPEC-012 改為 `rolePermission`。詳見 [選單導向授權模型](../選單導向授權模型/spec.md) 與 [角色權限維護](../../管理業務/系統管理作業/角色權限維護/spec.md)。

#### 原 contracts/menus.md — Contract: 選單設定 API（Menus）

REPR 端點；回傳型別採 Typed Results（憲章 API Response Conventions）。錯誤一律 RFC 7807 ProblemDetails。所有端點僅限「超級管理員」角色（FR-017）→ 未授權回 403。

##### POST /menus — 新增選單項目（US1, FR-001/002/007/018/019）

- Request：
  ```json
  { "name": "系統管理", "route": null, "icon": "settings", "displayOrder": 10, "parentId": 0 }
  ```
  - `name` 必填；`route` 可空（純容器父項，FR-019）；`parentId` 為整數，`0` 表示頂層。
  - 2026-09-30 修訂（設計決策 R8）：原契約寫「`parentId` 可空（`null`＝頂層）」，與實作（`int`、`0`＝頂層）不一致；依 Clarifications 2026-09-30 維持實作行為、契約對齊實作。資料庫改存 `NULL`，由用例入口轉換。
- Responses：
  - `201 Created` + Location `/menus/{id}` + 建立後的項目
  - `400` 驗證失敗（名稱空白、同層名稱重複 FR-007、造成循環 FR-018、父選單不存在 FR-020）
  - `403` 非超級管理員

##### PUT /menus/{id} — 編輯選單項目（US1, FR-003/005）

- Request：同上（可更新 name/route/icon/displayOrder/parentId）
- Responses：`200 Ok` + 更新後項目 / `404` 找不到 / `400` 驗證失敗（含循環）/ `403`

##### DELETE /menus/{id} — 刪除選單項目（US1, FR-004）

- 行為：含子項時依規則阻擋或連同刪除並提示；一併移除其 RoleMenuVisibility 授權。（FR-004 定案為阻擋。）
- Responses：`204 NoContent` / `404` / `400`（含子項且策略為阻擋）/ `403`

##### PUT /menus/reorder — 調整同層順序（US1, FR-005）

- Request：
  ```json
  { "parentId": 0, "orderedIds": [3, 1, 2] }
  ```
- Responses：`200 Ok` / `400`（id 不屬於該父層）/ `403`

##### PATCH /menus/{id}/enabled — 啟用/停用（US1, FR-006）

- Request：`{ "isEnabled": false }`
- Responses：`200 Ok` / `404` / `403`

##### GET /menus — 取得完整選單樹（US1）

- Response `200 Ok`：巢狀樹狀 DTO（含停用者，供管理檢視）
  ```json
  [ { "id": 1, "name": "系統管理", "route": null, "icon": "settings",
      "displayOrder": 10, "isEnabled": true,
      "children": [ { "id": 2, "name": "使用者", "route": "/admin/users",
                      "displayOrder": 10, "isEnabled": true, "children": [] } ] } ]
  ```
- `403` 非超級管理員

#### 原 contracts/roles.md — Contract: 角色 API（Roles）

REPR + Typed Results；錯誤 ProblemDetails。所有端點僅限「超級管理員」角色（FR-017）。

> 註：角色**無啟用/停用狀態**（Clarification Q5），僅軟刪除（`Removed`）。超級管理員以 `code='SUPERADMIN'` 辨識（取代 IsSystem 旗標）。

##### GET /roles — 角色清單（US2/US4, FR-008）

- Response `200 Ok`（分頁，預設上限 100，Principle XIV）：
  ```json
  [ { "id": 1, "code": "SUPERADMIN", "name": "超級管理員", "description": "系統內建" },
    { "id": 2, "code": "CS", "name": "客服", "description": "客服人員" } ]
  ```
- `403` 非超級管理員

##### POST /roles — 新增角色（US4, FR-014）

- Request：`{ "code": "CS", "name": "客服", "description": "客服人員" }`
- Responses：`201 Created` + Location `/roles/{id}` / `400`（代碼或名稱重複/空白）/ `403`

##### PUT /roles/{id} — 編輯角色（US4, FR-014）

- Request：`{ "name": "客服主管", "description": "..." }`
- 規則：`code='SUPERADMIN'` 的角色不可改為衝突值。
- Responses：`200 Ok` / `404` / `400` / `403`

##### DELETE /roles/{id} — 刪除角色（US4, FR-014）

- 規則：`code='SUPERADMIN'` 不可刪（FR-017，軟刪除）。
- 範圍外（FR-014a，待帳號系統整合）：仍有使用者指派時應阻擋並提示；本次不實作。（⚠️ 後由 SPEC-012 FR-012 實作「已指派不可刪」。）
- Responses：`204 NoContent` / `404` / `400`（超級管理員）/ `403`

#### 原 contracts/menu-visibility.md — Contract: 角色可見選單 & 執行期選單 API

REPR + Typed Results；錯誤 ProblemDetails。

##### GET /roles/{id}/menu-visibility — 取得某角色的可見設定（US2, FR-009/010）

僅限超級管理員（FR-017）。以選單樹呈現，每個「有路由的子項」標示是否已授權。

- Response `200 Ok`：
  ```json
  [ { "id": 1, "name": "系統管理", "route": null, "isContainer": true,
      "children": [ { "id": 2, "name": "使用者", "route": "/admin/users",
                      "isContainer": false, "granted": true },
                    { "id": 3, "name": "角色", "route": "/admin/roles",
                      "isContainer": false, "granted": false } ] } ]
  ```
- `404` 角色不存在 / `403`

##### PUT /roles/{id}/menu-visibility — 設定某角色的可見選單（US2, FR-009/010/011）

僅限超級管理員。以「被授權的有路由子項」清單覆寫該角色設定。容器不需傳入（自動顯示，FR-012）。

- Request：
  ```json
  { "grantedMenuItemIds": [2, 5, 6] }
  ```
- 規則：`grantedMenuItemIds` 只能是「具路由」的項目；含容器 id → `400`。
- Responses：`200 Ok`（回傳更新後可見樹）/ `404` / `400`（含容器 id 或不存在的 id）/ `403`

> ⚠️ 自 SPEC-012 起，請求須帶並行權杖 `rowVersion`（取自 GET 回應的 ETag）；缺漏回 400、過期回 409。容器判定自 SPEC-007 起改為「具子節點者」。

##### GET /me/menus — 目前使用者的可見選單樹（US3, FR-011/012/013/015）

任何已登入使用者皆可呼叫；回傳「該使用者所有角色授權子項的聯集 + 祖先容器，去除停用者」。

- Response `200 Ok`：
  ```json
  [ { "id": 1, "name": "系統管理", "route": null,
      "children": [ { "id": 2, "name": "使用者", "route": "/admin/users", "children": [] } ] } ]
  ```
  - 未授權子項不出現；父容器因其下有授權子項而自動顯示（FR-012）。
  - 無任何可見選單時回傳空陣列（Edge Case）。
- `401` 未登入

> ⚠️ 回應節點形狀自 SPEC-008 選單API欄位對齊前端 起改為 `text`／`value`／`subMenu`（葉節點無 `subMenu`）。

##### 契約驗證（Principle XV）

- 以 Verify 對 `/openapi/*.json` 及上述回應 DTO 做 snapshot；欄位/型別/狀態碼變更即 CI 失敗。

### 設計決策（原 research）

> 決策編號沿用原 research，供程式碼註解追溯。技術取向大多由憲章 v1.20.0 決定；此處只記錄需要研究/決策的點與其結論。

#### R1 選單階層的領域建模方式
- **決定**：領域層以 **TreeCollections**（`MutableEntityTreeNode<CategoryNode, MenuItemId, MenuItem>` 模式）建模選單樹；資料庫以自參考 FK（`ParentId`）持久化，載入後用 `AutoBuiltTreeSource` 從扁平清單建樹。
- **理由**：憲章 Technology Stack 已指定 TreeCollections；內建循環偵測（FR-018）、祖先/子孫走訪（FR-012 自動顯示容器）與 LINQ 過濾，避免自行手刻遞迴。
- **替代方案**：自寫 parent/child 導覽（被憲章排除）；閉包表（closure table，對此規模過度設計）。
- **實作偏差**：讀取路徑改用 `MenuTree` 純函式建樹與循環偵測；TreeCollections 保留供後續變更情境（原任務 T017）。

#### R2 權限（角色可見選單）的儲存與評估
- **決定**：以 `RoleMenuVisibility`（Role ↔ 具路由的 MenuItem 多對多）儲存授權；執行期取使用者所有角色的授權子項聯集，再用 TreeCollections 由子項回溯補上祖先容器後輸出樹。
- **理由**：FR-011/012/015——授權單位是「有路由的子項」，容器自動顯示，多角色聯集。預設拒絕＝不在表中即不可見。停用（`IsEnabled=false`）於輸出前過濾，實現「停用優先於授權」。
- **替代方案**：對每個節點（含容器）逐一授權（與 FR-012 自動顯示牴觸、設定負擔大）；以位元遮罩儲存（難維護、擴充性差）。

#### R3 超級管理員防鎖死
- **決定**：超級管理員以固定 `Code='SUPERADMIN'` 辨識（**取代原先規劃的 `IsSystem` 旗標**，避免多一欄）；種子資料建立一個 `Code='SUPERADMIN'` 的角色，領域不變量禁止刪除該角色（FR-017；角色無停用狀態）；兩個設定頁的授權檢查以「使用者是否具超級管理員角色」為準，與一般選單可見性機制分離。
- **理由**：避免 bootstrapping 鎖死（Clarify Q1=A）。分離授權來源，設定頁本身不受選單權限影響。
- **替代方案**：用選單權限保護設定頁（有鎖死風險，已否決）；帳號系統獨立 IsAdmin 旗標（與本功能角色模型重複，較不一致）。
- ⚠️ 已由 SPEC-005 選單導向授權模型取代（移除超管守門與防鎖死）。

#### R4 權限變更的生效時機
- **決定**：變更於使用者**下次載入頁面**（呼叫 `GET /me/menus`）時生效；不做即時推播。
- **理由**：FR-013；後台管理場景可接受，設計最簡，避免即時同步的複雜度。
- **替代方案**：WebSocket/SignalR 即時更新（對此需求過度設計）。

#### R5 選單「連結」的型態與驗證
- **決定**：`Route` 為可空的內部前端路由字串值物件；純容器父項可為 null（FR-019）。驗證僅檢查格式（相對路由樣式），不強制對應到已註冊頁面清單。初始資料由 SQL 種入。
- **理由**：Clarify Q2/Q3——連結即前端 route，容器可空。維護一份頁面註冊表非本次範圍。
- **替代方案**：從註冊頁面清單挑選（增加頁面登錄子系統，超出範圍）。
- **實作偏差**：`Route` 以受驗證的 nullable `string` 建模（避免 nullable-Vogen + EF 轉換摩擦，原任務 T015）。

#### R6 稽核與變更記錄
- **決定**：依 Principle XI/VI，於 `AuditableEntityBase` + `SaveChangesInterceptor` 自動填入 Create/Modify Date/User，滿足 FR-016，不在 handler 手動設定。
- **理由**：憲章既有機制，一致且防漏。
- **替代方案**：各 handler 手動寫入（易漏、與憲章牴觸）。

#### R7 前端樹狀元件與 UX 一致性
- **決定**：於 `shared/` 建立可重用的樹狀元件（選單編輯用可拖曳排序、角色權限用可勾選 checkbox tree），兩頁共用；完整狀態集（loading/empty/error/success）、鍵盤可操作（Principle XIII）。
- **理由**：兩頁都需選單樹呈現；共用避免分叉（Principle IX/XIII）。
- **替代方案**：兩頁各自實作（違反共用原則）。

#### 未解項
- 無阻斷性未解項。低影響項（大量選單的分頁/虛擬捲動）於資料規模不大時暫不處理，列為未來優化。

#### R8 資料表規範對齊：父選單外鍵與單數表名（2026-09-30；DATABASE-SCHEMA §5、§8；Clarifications 2026-09-30）
- **決定**：
  - `SysTask.ParentId` 改為可空的自參考外鍵（`MenuItemId?`，`NULL`＝頂層，`OnDelete: Restrict`），移除 `MenuItem.RootParentId`（0）哨兵；選單樹組裝、循環偵測、容器判定、種子改以 `null` 表示頂層。
  - API 契約不變：`POST /menus`、`PUT /menus/reorder` 的 `parentId` 仍為整數、`0`＝頂層，由用例入口轉為 `null`。
  - 新增選單時先驗證父選單存在（FR-020），不存在回 400 與中文訊息，不讓外鍵違反變成 500。
  - Migration：`ParentId` 改可空 → `UPDATE ... SET ParentId = NULL WHERE ParentId = 0` → 若仍有指向不存在列的 `ParentId`，以 `THROW` 中止（不自動修資料）→ 建外鍵與索引。
  - 表名改單數：`SysRoles`→`SysRole`、`SysTasks`→`SysTask`、`SysRoleTasks`→`SysRoleTask`，以 `RenameTable` 保留資料；主鍵、外鍵、索引名隨之改為 EF 預設命名（`PK_SysTask`、`IX_SysTask_ParentId_Name` 等）。
- **理由**：參照完整性應由資料庫保證（§5）；原 R1 即以自參考外鍵為設計，實作時改採 0 哨兵而無外鍵，本次回到原設計。表名依 §8 一律單數（使用者 2026-09-30 裁決，取代 2026-08 沿用既有 schema 表名的做法）。
- **替代方案**：
  - 保留 0 並建一筆「根」選單列讓外鍵可指向：多一筆假資料、選單樹與授權都要特別排除，得不償失。
  - API 一併改用 `null`：破壞性變更，前端（另一團隊）需同步改版（使用者裁決維持 0）。
  - Migration 自動把孤兒掛到頂層：可能讓選單出現在預期外的位置（使用者裁決中止）。
  - 表名對齊類別名（`SysMenuItem`、`SysRoleMenuVisibility`）：與舊系統 `SYS_TASKS` 的對應看不出來（使用者裁決保留 Task 字根）。
- 本次 migration `SingularSysTablesAndMenuParentFk` 一併將 SPEC-004 的 `SysUserRoles` 更名為 `SysUserRole`（該規格設計決策 R9）。

### 實作備註

> 原交接筆記（IMPLEMENTATION-NOTES）併入於此；為實作當時的狀態與慣例摘要，權威仍以本規格與憲章為準。

**如何執行**

- **Aspire（推薦）**：`dotnet run --project apphost/TcmClinic.Api.AspireHost`（v1.24.0：AspireHost 已移到最上層 `apphost/`；方案 `TcmClinic.Api.slnx` 與 CPM 在 repo 根）。會自動開 SQL Server 容器 + 乾淨庫 `ChbApi`（連線名 DefaultConnection），後端 `api`（TcmClinic.Api.Web）`WaitFor` 後套 migration + 種超級管理員；**前端已納入編排**（SPEC-003 前端納入Aspire開發編排）：後端就緒後以 `pnpm dev` 啟動 `CHB.Web`（資源名 `web`，埠由 Aspire 指派）。Dashboard 可看 sql／api／web；API 文件在後端的 `/scalar`（dashboard 的 api 資源有連結；根 `/` 是 404，正常）。
- **前端走 mock 或真後端**：`apphost/TcmClinic.Api.AspireHost/appsettings.Development.json` 的 `UseMock`——`true` 時不注入 `VITE_API_BASE_URL`，前端走 vite mock；`false`（或未設）時注入後端 http 端點，前端打真後端。前端也可脫離 Aspire 獨立 `pnpm dev`（回退 `/api`）。
- **直接跑 web（需本機 SQL 或指定連線）**：`ConnectionStrings__DefaultConnection` 指到一個**乾淨庫**（勿用既有 ChbApi，會與手動建的表撞名）。
- **測試**：xUnit v3 → 直接跑 exe（`bin/Debug/net10.0/*.exe`），`dotnet test` 需 MTP。功能測試需 Docker（Testcontainers）。前端 `npm run test`（vitest）。

**關鍵慣例 / 踩過的雷**

1. **Vogen 強型別 ID（Principle VII）**：非 IDENTITY，用 client-side `ValueGenerator`（Max+1）。EF config 需 `.HasSentinel(XxxId.From(0))`，實體 ctor 初始化 `Id = From(0)`（否則 EF 追蹤時對未初始化 Vogen 結構呼叫 GetHashCode 會拋錯）。Vogen 預設**不含** EfCoreValueConverter（會使 Core 依賴 EF，違反分層）→ 轉換在 Infrastructure 用 `HasConversion` 手寫。
2. **Mediator（martinothamar）**：handler 生命週期設 **Scoped**（`AddMediator(o => o.ServiceLifetime = Scoped)`，在 `UseCases.AddUseCases`），否則 handler（Singleton）依賴 Scoped Repository → captive dependency 於 ValidateOnBuild 爆掉。`Mediator.SourceGenerator` 在 UseCases 加 `PrivateAssets="all"`（避免跨組件重複產生）。
3. **EF/憲章**：時間欄 `datetime`（本地時間）；每表 `Removed` 軟刪除 + 全域查詢過濾器（AppDbContext 反射套用）；篩選唯一索引 `[Removed]=0`；稽核欄位由 `AuditableEntityInterceptor` 自動填、刪除轉軟刪除。
3b. **表名**：SysRoles / SysTasks / SysRoleTasks（領域 Role/MenuItem/RoleMenuVisibility 於 EF config 對應）。（⚠️ 2026-09-30 起改為單數 `SysRole`／`SysTask`／`SysRoleTask`，見設計決策 R8。）
4. **端點結果對應**：`Web/Endpoints/Menus/ResultMapping.ToProblem`（Ardalis.Result→ProblemDetails）。**Web 端點資料夾**依憲章 v1.23.0 收納於 `Web/Endpoints/{Menus,Roles,Navigation}/`（namespace `TcmClinic.Api.Web.Endpoints.*`），非直接放 Web 根。`IResult` 需寫全名 `Microsoft.AspNetCore.Http.IResult`（與 `Ardalis.Result.IResult` 衝突）。FastEndpoints v8 用 `Send.OkAsync`/`Send.ResultAsync`。
5. **授權**：端點套 `SuperAdmin` 原則。正式環境**尚未接驗證 scheme** → 直接呼叫 `/menus` 等會 **500**（無 scheme 可挑戰）。功能測試用 `CustomWebApplicationFactory` 的 `TestAuthHandler`（超級管理員）。`GET /me/menus` 只需已登入。**開發用 auth bypass 尚未加**（user 當時選擇先繼續做任務）。（⚠️ 授權模型後由 SPEC-005 改為選單可見性授權；登入驗證由 SPEC-004 使用者管理與登入 提供。）
6. **前端**：`@/` 別名（tsconfig paths，TS6 已移除 baseUrl）；shared 不得依賴 features（用通用 `TreeItem`/`CheckableTreeItem`/`NavNode` 契約）；vitest `vi.mock` 工廠**不可引用外部變數**（改斷言 mocked 模組）。
7. **migration**：`dotnet ef migrations add X -p src/TcmClinic.Api.Infrastructure -s src/TcmClinic.Api.Infrastructure`（用設計時 factory）。工具在 `.config/dotnet-tools.json`（dotnet-ef、dotnet-stryker）。

**已知未解落差（當時需使用者決策）**

- **EF 欄位名 vs 契約 schema（已對齊）**：EF 未設 `HasColumnName`，欄名採領域屬性名 → `Code/Name`、`Name/Icon/DisplayOrder`、`Route`；資料庫綱要與資料模型已更新對齊此 as-built（analyze D1）。**若要對接現成資料庫**（欄名 `RoleCode/RoleName/TaskName/IconName/TaskSort`），在 `RoleConfig`/`MenuItemConfig` 為對應屬性加 `.HasColumnName("RoleCode")` 等並重新產生 migration；領域值物件型別本就叫 `RoleCode/RoleName/MenuItemName`，僅欄名不同。
- **/menus 正式環境 500**：需接驗證機制或加開發 bypass 才能在瀏覽器/前端實際操作受保護端點。（現況：SPEC-004 已於 `Configurations/AuthConfigs.cs` 接上 Cookie 驗證，此落差不再適用。）
- **FR-014a「仍有使用者指派時阻擋刪除角色」（範圍外）**：使用者↔角色指派屬既有帳號系統，無資料可判，DeleteRole 只做了「SUPERADMIN 不可刪」（FR-014）。（後由 SPEC-012 FR-012 實作。）

**變異測試（原任務 T045，已移除為待辦）**

Principle XV 的 Stryker 變異測試屬 SHOULD、非阻斷；設定保留於 `backend/stryker-config.json`。

- **權威結論（已實證定論）**：正確參數（`dotnet stryker --project TcmClinic.Api.Core.csproj`，於測試專案目錄、預設覆蓋率、**不加** `--test-project`、**不加** `coverage-analysis off`）跑整個 Core → **Killed 0 / Timeout 2 / Survived 96 → 2.04%**。只有逾時（process 層級）被偵測，assertion kill=0。**決定性實驗**：手動注入 mutant 31（`MenuTree` `OrderBy`→`OrderByDescending`）直接跑測試 exe → 對應測試 **Failed**（測試有效）；但 Stryker 對同一 mutant 判 survived → **Stryker 4.16 的 VsTest runner 讀不到 xUnit v3/MTP 的測試失敗**（只抓逾時）。**非測試品質問題。** 先前 0% 是另外加了 `--test-project`／`coverage-analysis off` 造成的雜訊，已排除；根因是 runner 相容性。處置：待 Stryker 支援 MTP 原生 runner，或評估改走傳統 VSTest（與憲章 MTP 取向衝突）。
- 調查歷程（已被上述結論取代）：
  - 根因 1（本機卡關）：Stryker 預設建置整個方案（含 Web），而 Web 輸出 DLL 被**執行中的 Aspire App 鎖住** → 初始建置失敗。修法：在**應用未執行**時跑（CI 天然滿足），或只針對測試專案範圍跑（不建置 Web）。
  - 根因 2（score 0%）：Stryker 4.16 的覆蓋率收集器在 xUnit v3 / MTP 下疑似掛不上，導致 kill 無法對應 → 全數誤判存活。修法：config 曾設 `"coverage-analysis": "off"`。
  - 本機可用指令（app 執行中時，從測試專案資料夾跑，不建置 Web）：`cd backend/tests/TcmClinic.Api.UnitTests; dotnet stryker --project TcmClinic.Api.Core.csproj --test-project TcmClinic.Api.UnitTests.csproj --concurrency 4`
  - CI 指令（app 未執行，用 repo 設定）：於 `backend/` 執行 `dotnet stryker`（讀 `stryker-config.json`）。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)（當時 v1.24.0）：無設計違規、無需 Complexity Tracking。Clean Architecture 四層（NetArchTest 強制）、DDD 聚合封裝不變量（唯一性、循環阻擋、停用優先）、CQRS + Mediator、分層驗證、測試先行與 property-based、SaveChangesInterceptor 稽核、Vogen 強型別 ID、繁中撰寫、前端功能導向、DB 稽核欄位與 `Removed` 軟刪除、效能（AsNoTracking、避免 N+1、分頁上限）、新相依登記 THIRD-PARTY-NOTICES 皆符合。憲章後續合規（實作層級，非設計違規）：v1.22.0 測試意圖式註解回補（原任務 T050–T052）；v1.23.0 Web 端點切片由 `Web/` 根重構至 `Web/Endpoints/`（T053）；v1.24.0 AspireHost 移到 `apphost/TcmClinic.Api.AspireHost`、`.slnx` 與 `Directory.Packages.props`/`Build.props` 上移 repo 根、`ServiceDefaults` 留 `backend/src`（T054；驗證：`dotnet build TcmClinic.Api.slnx` 0/0、UnitTests 19、FunctionalTests 14 全綠）。2026-09-30 修訂（設計決策 R8）修正 as-built 與 DATABASE-SCHEMA 的兩處不符：`SysTasks.ParentId` 以 0 表示頂層而未建自參考外鍵（§5）、三張表名為複數（§8）。

## Test Plan（測試計畫）

### 自動化測試

- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/MenuAggregate/MenuItemTests.cs`、`MenuTreeTests.cs`、`MenuHandlerTests.cs`、`MenuVisibilityTests.cs`：選單不變量（CsCheck property）、建樹與循環偵測、用例（同層唯一、父選單不存在、刪除阻擋、`parentId = 0` 轉 `null`）、可見性解析（祖先容器、停用優先、聯集、預設拒絕）。
- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/Authorization/GetVisibleMenuRoutesTests.cs`：多角色可見路由聯集。
- [x] 架構測試 `backend/tests/TcmClinic.Api.UnitTests/Architecture/ArchitectureTests.cs`：Core／UseCases 不依賴 Infrastructure／Web。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/MenuRoleSchemaTests.cs`、`MenuParentFkMigrationTests.cs`：單數表名、`ParentId` 自參考外鍵、頂層名稱唯一、migration 資料轉換與孤兒中止。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbMenuTreeSeedTests.cs`（`Reseeding_removes_retired_admin_menus`）：舊後台選單軟刪除。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/MenuEndpointsTests.cs`、`RoleVisibilityEndpointsTests.cs`、`NavigationEndpointsTests.cs`、`RoleCrudEndpointsTests.cs`、`MenuClaimsRevalidationTests.cs`：端點行為（真實 SQL 容器）。
- [x] 前端 Vitest：`frontend/src/features/menu-settings/menu.store.spec.ts`、`frontend/src/features/role-permissions/role.store.spec.ts`。

執行（範例）：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.UnitTests -- -class "TcmClinic.Api.UnitTests.MenuAggregate.MenuVisibilityTests"
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests -- -class "TcmClinic.Api.FunctionalTests.MenuEndpointsTests"
```

### 手動走查

前置：Docker Desktop 執行中；已套用 EF Core migrations 並種入 `Code='SUPERADMIN'` 角色 + 初始選單。開發環境可查看 `/scalar`（API 參考）、`/openapi/*.json`。以下示範資料（「系統管理」「使用者 `/admin/users`」「角色 `/admin/roles`」「客服」）僅為走查用；授權依 SPEC-005 以選單可見性判定（開發模式 `admin` 經 SPEC-009 可見全部選單）。

1. 啟動：`dotnet run --project apphost/TcmClinic.Api.AspireHost`（原文寫 `backend/src/{Project}.AspireHost`，v1.24.0 起已移至 `apphost/`）；前端另以 `pnpm dev` 或經 Aspire 啟動。
2. **US1**：以超級管理員（開發模式 `admin`）登入 → 進入「選單設定頁」。
3. 新增父項「系統管理」（route 留空）與子項「使用者」（`/admin/users`）、「角色」（`/admin/roles`）；編輯其中一項名稱或圖示。
4. 調整順序、將「角色」停用。**預期**：樹狀結構正確；`GET /menus` 反映階層與狀態；重複同層名稱或造成循環時被擋（400）。
5. **US2**：進入「角色權限設定頁」，新增角色「客服」。
6. 選取「客服」，於選單樹勾選「使用者」子項，儲存。
7. **預期**：`GET /roles/{id}/menu-visibility` 顯示「使用者」granted=true、「角色」granted=false；傳入容器 id 會被拒（400）。
8. **US3**：指派某測試使用者為「客服」角色；以該使用者呼叫 `GET /me/menus`。**預期**：只看到「使用者」；父容器「系統管理」因含授權子項而**自動顯示**；停用項不出現；未授權項可見率 0%（SC-004）。多角色時為聯集（FR-015）。
9. **US4**：嘗試刪除「超級管理員」角色。**預期**：被阻擋（400）——原依 FR-017「`code='SUPERADMIN'` 不可刪除」，現行依 SPEC-012「已指派不可刪」。
10. 建立一般角色 → 出現在清單並可被指派可見選單。（範圍外，FR-014a：仍有使用者指派時阻擋刪除，本次不實作。）
11. **效能（Principle XIV）**：`GET /menus`、`GET /me/menus` 讀取 ≤ 300 ms p95；寫入端點 ≤ 500 ms p95；選單樹查詢無 N+1。

## Dependencies（相依）

- 憲章（Clean Architecture、DDD、Vogen、TreeCollections、Principle XI 稽核欄位與軟刪除）。
- 既有帳號系統（使用者↔角色指派；後由 SPEC-004 使用者管理與登入 提供）。
- 下游：SPEC-004、SPEC-005、SPEC-007、SPEC-008、SPEC-009、SPEC-012 皆沿用本規格的 `SysRole`／`SysTask`／`SysRoleTask` 資料模型。

## Risks（風險）

- 本規格多處（超管守門、防鎖死、角色刪除保護、授權鍵、`/me/menus` 回應形狀、可見性並行權杖）已被後續規格修改，閱讀時須搭配表頭註記與 SPEC-005／008／012。
- 變異測試（Stryker.NET 4.16）因 runner 與 xUnit v3/MTP 不相容而無法產生有效分數，暫無變異測試把關。
- 2026-09-30 migration 遇孤兒父選單即中止，需人工修資料後才能升級（使用者明示接受）。
- 角色可見性的「含子項」整層套用（FR-010 SHOULD）未實作。

## 實作紀錄（Archive）

- **狀態**：已實作並收斂，所有任務完成（原 T045 變異測試因工具相容性移除為待辦）。
- **主要提交**：
  - 規格：`8bbb7a7` 新增規格與設計產物 → `981717c` 一致性修正（角色停用移除、父選單刪除策略、稽核界定、FR-008 歸位 US2）。
  - 實作：`b04934e`／`7f1e6db` Setup 與 Foundational（T001–T011）→ `08ddb0e`、`6e27cd6`、`fe37010` US1（T012–T024）→ `d65926a` Aspire SQL 修正 → `15b9555` US2（T025–T033、T029a）→ `c640583` US3（T034–T038）→ `967b38a` US4（T039–T043）→ `362ce5e` Phase 7 Polish（T044、T046–T049）。
  - 憲章合規：`3bb0209`、`64a1156`（v1.23.0，T053）→ `2c76154`（v1.22.0 測試意圖式註解，T050–T052）→ `e4701af`、`d0d2de9`（analyze 修正）→ `dcedf00`（移除 T045）→ `3c2ff85`、`6c4950b`（v1.24.0，T054）。
  - 被 SPEC-005 取代之 FR 加前向註記：`d387981`。
  - 2026-09-30 資料表規範對齊：`3f2ed74` 規格 → `d42bdef` 收斂任務 T055–T059 → `e5d3f1b` 先失敗測試 → `63c9b7f` 實作 → `97a56cc` 完成（全套 182 單元 + 92 整合 + 206 功能綠）。
  - 2026-10-02 舊後台選單移除：`c8e5872`。
- **測試數**：初版 UnitTests 19 + FunctionalTests 14 全綠（T048、T054）；2026-09-30 修訂後全套 182 單元 + 92 整合 + 206 功能綠。
- **審查紀錄**：規格品質檢查（原 `checklists/requirements.md`）全數通過（2026-08-26），以合理預設完成規格、未留待釐清標記。
- **原 Spec Kit 產出**：plan、research、data-model、quickstart、tasks、checklists、contracts（menus、roles、menu-visibility、database-schema.sql）與 IMPLEMENTATION-NOTES 已併入本檔後移除；原始檔可由 `git show 97a56cc:"specs/其他/後台選單與角色權限管理/tasks.md"` 等方式追溯（IMPLEMENTATION-NOTES 最後版本見 `db7962c`）。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：2026-10-01 資料表規範對齊修訂的紅燈 commit：`33a090f`（與 SPEC-004 共用，T055–T056）。初版實作沒有單獨提交紅燈 commit，無紀錄可補。
