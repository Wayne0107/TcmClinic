# [SPEC-009] Feature: 開發模式可見全部選單

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-009 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 無（跨選單基礎規格） |
| 建立日期 | 2026-09-07 |
| 最後更新 | 2026-10-04（由 Spec Kit 格式轉為 UDS SDD 格式；內容未變） |
| 原分支 | `009-sm-all-menus-dev` |
| 相關規格 | SPEC-005 選單導向授權模型（SUPERADMIN 定義）、SPEC-007 CHB選單與角色種子（選單樹與葉節點授權） |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

> **修訂說明（2026-09-07）**：本規格初版誤將目標設為 CHB 的 `SM` 角色；但「開發模式登入的系統管理員」實際是 **SUPERADMIN**（帳號 `admin`、UserName「系統管理員」，SPEC-005 內建、僅 Development 種入）。而 `SM` 是 CHB 五角色之一、**無任何使用者指派**，改它無法被登入驗證。已修訂為：由 **SUPERADMIN** 於開發模式可見全部選單。`SM` 回復 SPEC-007 的受限定義。

## Overview（概述）

開發者以開發模式的系統管理員帳號（`admin`，SUPERADMIN）登入時，側邊選單呈現系統中全部選單項目。範圍限於後端種子的可見性：不修改前端 `useMenu.ts`；種子只在 Development 執行；維持冪等，並與既有 005／007 資料共存、不刪除。

## Motivation（動機）

開發與驗證時若要逐一切換 BH／BS／SM／MB／BM 等角色帳號才能看到各功能頁，成本很高。讓 SUPERADMIN 在開發環境直接看到整棵選單樹，即可在單一帳號下檢視與操作任一功能頁面；同時必須確保這個便利不外溢到真實 CHB 角色的授權邊界。

## Clarifications（釐清紀錄）

### Session 2026-10-02

背景：舊後台選單 `/admin/users`、`/admin/roles`、`/admin/audit-logs`、`/admin/access-logs` 已移除（見 CHB選單與角色種子 Clarifications Session 2026-10-02）。

- Q: SUPERADMIN「可見全部選單」的範圍是否變動？ → A: 規則不變（全部未刪除葉節點）；舊後台選單軟刪除後自然不再包含，`/admin/*` 只剩 `/admin/menus`。本規格中「005 內建管理選單（`/admin/*`）」自此僅指 `/admin/menus`。

## Requirements（需求）

### Requirement: US1 開發者以 admin 登入即見全部選單（P1）

The system SHALL 讓 SUPERADMIN 角色在 Development 環境的可見選單涵蓋整棵合併選單樹的全體葉節點（含 005 內建管理選單 `/admin/*` 與全部 CHB 功能項），且每個葉節點都可經由容器鏈展開到達。

這是本功能唯一的核心價值；單獨交付即構成可用成果。涵蓋 FR-001、FR-003、FR-004、FR-006。

#### Scenario: AC-1 SUPERADMIN 可見全體葉節點
- **GIVEN** Development 環境且 seed 已執行
- **WHEN** 以 SUPERADMIN 角色查詢可見選單
- **THEN** 回傳的可見葉節點集合等於整棵合併樹的全體葉節點（去重）

#### Scenario: AC-2 可見葉節點皆可展開到達
- **GIVEN** 上述可見葉節點
- **WHEN** 檢視選單樹結構
- **THEN** 每個可見葉節點皆能自根節點經由其父容器鏈展開到達（不出現孤兒葉節點）

#### Scenario: AC-3 重跑種子不產生重複列
- **GIVEN** seed 已執行一次
- **WHEN** 重複執行 seed
- **THEN** 不新增重複的角色可見性列（冪等）

### Requirement: US2 CHB 角色可見範圍不受影響（P2）

The system SHALL 維持 BH／BS／SM／MB／BM 等 CHB 角色的可見選單範圍與 SPEC-007 定義一致，不因 SUPERADMIN 擴權而改變。

確保開發便利不外溢污染真實 CHB 角色的授權邊界。涵蓋 FR-005。

#### Scenario: AC-4 CHB 角色可見集合不變
- **GIVEN** seed 已執行
- **WHEN** 查詢 BH／BS／SM／MB／BM 任一角色的可見選單
- **THEN** 其集合與 SPEC-007 所定義者相同

### 功能需求條文（Functional Requirements）

- **FR-001**: 系統 MUST 於後端種子資料中，將 SUPERADMIN 角色的可見選單設定為整棵合併選單樹的全體葉節點（去重），涵蓋 005 內建管理選單（`/admin/*`）與全部 CHB 功能項。
- **FR-002**: 系統 MUST NOT 修改前端 `CHB.Web/src/composables/useMenu.ts`。
- **FR-003**: SUPERADMIN 的擴權 MUST 僅在 Development 環境的種子流程生效，與既有 005/007 種子共存、不刪除既有資料。
- **FR-004**: 種子流程 MUST 維持冪等；重複執行不得產生重複的角色可見性列。
- **FR-005**: 本次變更 MUST NOT 改變 BH／BS／SM／MB／BM 任一 CHB 角色既有的可見選單範圍（SM 維持 007 受限子集）。
- **FR-006**: 可見性 MUST 只授予葉節點；不為容器節點種可見性列（沿用 007 決策 4，可展開性由 ParentId 鏈保證）。
- **FR-007**: 程式碼註解 MUST 記錄「SUPERADMIN＝全部選單」是開發模式便利決策及其理由（含為何選 SUPERADMIN 而非 CHB SM）。

### 邊界情境（Edge Cases）

- **非開發環境**：seed 僅於 Development 執行；正式環境不落地此擴權（同既有 007/005 種子策略）。
- **前端**：不修改前端 useMenu.ts；「看全部」僅存在於後端 seed 授予 SUPERADMIN 的可見性。
- **未來新增選單**：當有新選單葉節點加入樹時，SUPERADMIN 的「全部」定義須自動涵蓋之，不應遺漏。
- **容器 vs 葉節點**：可見性只授予葉節點；可展開性由既有 ParentId 鏈保證，不為容器種可見性列（沿用 007 決策 4）。

### 關鍵實體（Key Entities）

- **角色（Role）**：以 RoleCode 識別；本功能聚焦 `SUPERADMIN`（內建超管，開發登入帳號 `admin` 所屬）。
- **選單項目（MenuItem）**：樹節點，含容器與葉節點；葉節點＝無子節點者。
- **角色可見選單（RoleMenuVisibility）**：(RoleId, MenuItemId) 關聯；本功能為 SUPERADMIN 補齊「整棵樹全體葉節點」對應的關聯列。

### 假設（Assumptions）

- 「全部選單」的權威來源為後端已種入的整棵合併選單樹（005 `/admin/*` + 007 之 CHB 合併樹）。
- SUPERADMIN 在正式環境不預期擁有此全量可見性；本功能之「看全部」純為開發／驗證便利。
- 沿用 007 的節點模型與冪等策略，不新增資料庫欄位、不改 schema。
- 種子僅於 Development 執行（沿用 `Program.cs` 既有 `IsDevelopment()` 守門）。

## Acceptance Criteria（驗收條件）

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、SC-001 | SUPERADMIN 可見葉節點數＝整棵樹全體葉節點去重數（0 遺漏） | `backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbVisibilitySeedTests.cs` › `Super_admin_sees_all_live_leaves` |
| AC-2 | US1 情境 2、SC-002 | 每個可見葉節點可自根經容器鏈到達（孤兒葉節點數＝0） | `ChbVisibilitySeedTests` › `Visible_leaf_ancestors_are_live` |
| AC-3 | US1 情境 3、SC-003 | 重跑 seed 後可見性列數不增加（重複列＝0） | `ChbVisibilitySeedTests` › `Reseeding_does_not_duplicate_visibility` |
| AC-4 | US2 情境 1、SC-004 | BH／BS／SM／MB／BM 可見葉節點集合與 SPEC-007 定義相同（差異＝0） | `ChbVisibilitySeedTests` › `Chb_roles_get_only_implemented_feature_grants` |
| AC-5 | FR-006 | 可見性只授予葉節點，容器不入表 | `ChbVisibilitySeedTests` › `Visibility_is_granted_to_leaves_only` |
| AC-6 | FR-002、FR-007 | 未修改前端 `useMenu.ts`；種子程式碼以註解記錄選 SUPERADMIN 的理由 | 手動：Code Review 確認 `ChbMenuSeedData.cs` 的 `[開發模式便利 feature 009]` 註解存在（commit `a9de180`） |

> AC-4 的「SPEC-007 定義」自 SPEC-007 FR-016（2026-10-01）起為「CHB 角色只授予已實作功能所需的葉節點」，驗證測試已隨之更新。

## Technical Design（技術設計）

### 技術脈絡

- 語言／框架：C# / .NET，沿用現行後端。
- 相依：EF Core、既有 `AppDbContext`、`SeedData`／`ChbMenuSeedData`。
- 儲存：SQL Server 既有 `MenuItem`、`Role`、`RoleMenuVisibility` 資料表；**無 schema 變更**。
- 測試：xUnit 整合測試（Testcontainers MsSql）。

### 受影響元件

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.Infrastructure/Data/ChbMenuSeedData.cs` | 唯一生產碼修改點：`SeedVisibilityAsync` 在各 CHB 角色照 007 授其自身葉節點後，額外對 SUPERADMIN 授予全體葉節點 |
| `backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbVisibilitySeedTests.cs` | 新增 SUPERADMIN 全葉與其他角色不受影響的測試（共用既有容器與 helper） |

明確不觸及 `CHB.Web/src/composables/useMenu.ts`（前端由另一團隊維護，本流程只涵蓋後端）。

### 資料模型

無 schema 變更；只改變種子在 `RoleMenuVisibility` 為 SUPERADMIN 寫入的資料列集合。沿用 002／005／007 既有實體：

- **Role**：以 `RoleCode`（Vogen）識別；不新增或修改角色定義。
- **MenuItem**：樹節點；葉節點＝無子節點者（由 `CollectLeafValues` 深度走訪判定）。`Route`（＝前端 value）為節點鍵，`ParentId` 構成可展開鏈。不新增或修改節點。
- **RoleMenuVisibility**：`(RoleId, MenuItemId)` 關聯，也是冪等鍵（已存在則略過）。本功能唯一的資料變化點。

| 規則 | 內容 | 對應需求 |
|------|------|---------|
| 全葉聯集 | SUPERADMIN 可見葉集合＝⋃(所有角色樹葉節點)，去重 | FR-001、SC-001 |
| 只授葉 | 不為容器建可見性列 | FR-006 |
| 可達性 | 每個可見葉之祖先容器皆存在（ParentId 鏈完整） | SC-002 |
| 冪等 | 重跑不新增重複 `(RoleId, MenuItemId)` 列 | FR-004、SC-003 |
| 隔離 | CHB 角色可見集合不受影響 | FR-005、SC-004 |

無狀態機；種子為一次性冪等寫入。

### API 契約

無變更。`/me/menus` 的回應欄位沿用 SPEC-008。

### 設計決策（原 research）

> 決策編號沿用原 research.md，供程式碼註解追溯。決策 1、4 原以 `SM` 撰寫，實作已依修訂說明改為 SUPERADMIN。

#### 決策 1：「全部選單」＝全體角色葉節點的去重聯集
- **決定**：可見性＝對 `AccordionMenu` 全部角色樹收集葉節點（`CollectLeafValues`）後去重聯集，逐一建 `RoleMenuVisibility`。
- **理由**：選單真相來源（useMenu.ts 於 007 的快照）已合併為單一全域樹並種入；「全部」即該樹的全體葉節點。以「全體角色葉聯集」定義可自動涵蓋任何角色出現過的葉節點，未來新增角色或葉節點時亦自動涵蓋（SC-001）。
- **替代方案**：
  - 掃 `MenuItems` 全表取所有葉節點：可行，但與 007 以 `AccordionMenu` 為來源的種入風格不一致，且需在 DB 端判定葉／容器。聯集法直接複用既有 `CollectLeafValues`。
  - 授予容器＋葉全部：違反 007 決策 4（只授葉節點），且容器可見性非必要。否決。

#### 決策 2：僅後端 seed，不改前端 useMenu.ts
- **決定**：只調整 `ChbMenuSeedData.SeedVisibilityAsync`；前端 `useMenu.ts` 維持不變。
- **理由**：「看全部」純為開發／驗證便利；本流程只涵蓋後端，前端由另一團隊維護。刻意讓後端 seed 偏離 007「忠實鏡射」原則，此偏離須於程式碼註解與規格記錄（FR-007、FR-002）。
- **替代方案**：同步改 useMenu.ts 讓前端也看全部 → 會污染真實角色定義、逾越後端範圍。否決（亦為使用者明確選擇）。

#### 決策 3：只授葉節點、維持冪等、僅 Development
- **決定**：沿用 007 既有機制——只對葉節點建可見性列；以 `(RoleId, MenuItemId)` 去重確保冪等；種子整體僅於 `Program.cs` 的 `IsDevelopment()` 區塊執行。
- **理由**：與既有 005/007 資料共存、重跑不產生重複列（SC-003）；正式環境不落地此擴權。
- **替代方案**：無；此為既有慣例的直接沿用。

#### 決策 4：其他角色可見範圍不變
- **決定**：特判僅作用於 SUPERADMIN；CHB 角色走原路徑（各自樹葉）。
- **理由**：避免擴權外溢污染其他角色授權邊界（FR-005、SC-004），以「變更前後其他角色可見葉集合相等」的整合測試守護。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)：無違規。種子具分支邏輯，以真實 DB 整合測試驗收（測試優先）；沿用既有強型別 ID 與資料表；特判屬刻意偏離既有原則，以規格式區塊註解記錄理由。

## Test Plan（測試計畫）

### 自動化測試

- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/ChbVisibilitySeedTests.cs`：AC-1～AC-5（見驗收條件表）。

執行：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests -- -class "TcmClinic.Api.IntegrationTests.Data.ChbVisibilitySeedTests"
```

預期：全綠；SUPERADMIN 可見葉數＝全體去重葉數，其他角色差異為 0。

### 手動走查

1. 於 Development 啟動後端（種子自動執行），以 `admin` 登入前端或呼叫 `/me/menus`。
2. 應回傳合併全域樹的全部葉節點，各業務容器均可展開至功能頁。

## Dependencies（相依）

- SPEC-005 選單導向授權模型：SUPERADMIN 角色與 `/admin/*` 內建選單。
- SPEC-007 CHB選單與角色種子：合併選單樹、`CollectLeafValues`、只授葉節點（決策 4）與冪等策略。
- SPEC-008 選單API欄位對齊前端：`/me/menus` 回應契約。

## Risks（風險）

- 刻意偏離 007「忠實鏡射前端選單」原則；以 FR-007 要求的程式碼註解與 AC-4 隔離測試控制。
- 擴權只靠 `IsDevelopment()` 守門；若日後在非 Development 環境執行種子，須重新評估此規則。

## 實作紀錄（Archive）

- **狀態**：已實作並收斂，所有任務完成。
- **主要提交**：`fe6ff28` 初版（SM）→ `e66bae1` 改為 SUPERADMIN → `0e8bca7` 合併 → `a9de180` 種子註解補上選 SUPERADMIN 而非 SM 的理由（收斂任務 T014）。
- **審查紀錄**：規格品質檢查（原 `checklists/requirements.md`）全數通過，於 2026-09-07 核准進入實作。
- **原 Spec Kit 產出**：plan.md、research.md、data-model.md、quickstart.md、tasks.md、checklists/ 已併入本檔後移除；原始檔可由 `git show a9de180:"specs/其他/開發模式可見全部選單/tasks.md"` 等方式追溯。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：沒有單獨提交紅燈 commit，無紀錄可補。
