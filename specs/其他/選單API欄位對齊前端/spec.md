# [SPEC-008] Feature: 選單 API 回傳欄位對齊前端（text / value / subMenu）

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-008 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 無（跨選單基礎規格） |
| 建立日期 | 2026-09-07 |
| 最後更新 | 2026-10-04（由 Spec Kit 格式轉為 UDS SDD 格式；內容未變） |
| 原分支 | `008-menu-api-field-align` |
| 相關規格 | SPEC-005 選單導向授權模型（依角色可見性回傳選單之機制）、SPEC-007 CHB選單與角色種子（選單樹與可見性資料，`value` 存於 `Route`）；← SPEC-009 開發模式可見全部選單（沿用本規格 `/me/menus` 回應契約；SUPERADMIN 可見全部葉節點，影響本規格契約快照內容）、SPEC-012 角色權限維護（依賴本規格回應欄位） |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

> **狀態沿革**：原規格頂端狀態為 Draft；實作已於 2026-09-07 完成並合併（`3487091`），規格索引記為「✅ 已完成（全套 132 測試綠）」，故本檔狀態為 Implemented。
>
> ⚠️ **被後續變更影響的註記**：
> - SPEC-009（2026-09-07）讓種子 admin（SUPERADMIN）可見全部葉節點，`/me/menus` 契約快照因此由原本僅三個 `/admin/*` 的最小 fixture 變為整棵 CHB 選單樹；回應形狀規則不變。
> - 2026-10-02 舊後台選單 `/admin/users`、`/admin/roles`、`/admin/audit-logs`、`/admin/access-logs` 移除（見 SPEC-007 Clarifications Session 2026-10-02），`/admin/*` 只剩 `/admin/menus`；本規格回應形狀不受影響。

## Overview（概述）

將後端回傳給前端的使用者可見選單樹（`GET /me/menus`）在欄位形狀上對齊前端 `CHB.Web/src/composables/useMenu.ts` 的 `accordionMenu` `MenuItem` 型別：每個節點只含 `text`（顯示名稱）、`value`（識別鍵）、`subMenu`（子節點陣列，葉節點省略），移除 id、icon、顯示順序、啟用狀態等多餘欄位，使前端可直接使用、不需再做欄位轉換。資料表不變，可見性判定不變；`/menus`（選單管理頁）維持完整欄位，兩端點改用各自的回應型別。

原始需求：「參考 CHB.Web/src/composables/useMenu.ts，有關選單的『後端傳前端』部分，回傳欄位要對齊截圖（accordionMenu 的 text / value / subMenu），不必要的欄位可以拿掉；資料表可以不用改。」

## Motivation（動機）

- **US1 欄位對齊（P1）**：這是本功能的核心價值——消除前後端選單欄位落差；沒有它，前端仍需自行轉換。
- **US2 移除不必要欄位（P2）**：精簡 payload 並避免前端型別出現用不到的欄位；依賴 US1 的欄位命名到位。

## Clarifications（釐清紀錄）

### Session 2026-09-07

- Q: 本功能應**改變既有 `/me/menus` 的回應欄位形狀**（會連帶影響 005 既有前端消費者、功能測試與契約快照），還是**新增一個對齊前端形狀的獨立端點**（既有端點不動）？ → A: **改既有 `/me/menus`**。回應改為 `{text, value, subMenu}` 單一形狀；MUST 一併更新受影響之後端功能測試與契約快照（Verify），既有前端（005）之對應調整以 API 契約交付前端團隊。
- Q: 欄位對齊要只套用在 `/me/menus`，還是連 `/menus`（選單管理頁）也一起改？ → A: **只對齊 `/me/menus`**。`/menus`（`GetMenuTreeEndpoint`，選單管理頁編輯用）維持完整欄位（id/route/icon/顯示順序/啟用狀態）不變；為 `/me/menus` 另立精簡回應型別，兩端點不再共用同一回應型別。
- Q: 沒有子項的葉節點，回傳時要省略 `subMenu` 鍵還是回傳空陣列？ → A: **完全省略 `subMenu` 鍵**（與前端 accordionMenu 原樣一致；僅容器節點含 `subMenu`）。

## Requirements（需求）

### Requirement: US1 選單回傳欄位對齊前端型別（P1）

The system SHALL 讓 `GET /me/menus` 回傳的每個選單節點以 `text`（顯示名稱）、`value`（識別鍵）、`subMenu`（子節點陣列）表示，與前端 `accordionMenu` 的 `MenuItem` 型別（`{ text, value, subMenu? }`）完全一致。

前端開發者呼叫「取得選單」API 後無需再做欄位改名或結構轉換即可套用。獨立驗證方式：回傳 JSON 每個節點只含 `text`/`value`/`subMenu`，且巢狀結構與前端型別相符（可直接反序列化為前端 `MenuItem[]`）。涵蓋 FR-001、FR-002、FR-003、FR-005、FR-006、FR-008、FR-009。

#### Scenario: AC-1 節點以 text／value 表示
- **GIVEN** 後端有一棵選單樹
- **WHEN** 前端呼叫選單 API
- **THEN** 每個節點以 `text` 表示名稱、`value` 表示識別鍵

#### Scenario: AC-2 子節點置於 subMenu
- **GIVEN** 某節點有子節點
- **WHEN** 檢視回傳
- **THEN** 子節點置於 `subMenu` 陣列並遞迴同樣形狀

#### Scenario: AC-3 葉節點不含 subMenu
- **GIVEN** 某節點無子節點（葉）
- **WHEN** 檢視回傳
- **THEN** 該節點不含 `subMenu`（或為空），與前端 optional `subMenu?` 一致

> AC-3 的「（或為空）」已由 Clarifications 第 3 題與 FR-003 收斂為**完全省略 `subMenu` 鍵**。

### Requirement: US2 移除不必要欄位（P2）

The system SHALL 讓 `/me/menus` 回傳的節點只保留 `text`、`value`、`subMenu`，移除節點內部 id、圖示、顯示順序、啟用狀態等多餘欄位。

精簡由 US1 的專屬回應型別達成，本故事以「鍵集合斷言」獨立驗證：回傳 JSON 的節點鍵集合恰為 `{text, value, subMenu?}`。涵蓋 FR-004、FR-010。

#### Scenario: AC-4 節點鍵僅 text／value／subMenu
- **GIVEN** 選單 API 回應
- **WHEN** 檢視任一節點的欄位鍵
- **THEN** 僅有 `text`、`value`、以及（若有子項）`subMenu`

#### Scenario: AC-5 舊欄位不再出現
- **GIVEN** 原本回傳含 id/icon/displayOrder/isEnabled
- **WHEN** 對齊後
- **THEN** 這些欄位不再出現於回應

### 功能需求條文（Functional Requirements）

- **FR-001**: 選單 API 回應中，每個節點 MUST 以 `text` 表示顯示名稱、以 `value` 表示識別鍵。
- **FR-002**: 節點的子節點 MUST 以 `subMenu` 陣列巢狀表示，並遞迴採用相同節點形狀。
- **FR-003**: 無子節點的葉節點 MUST **完全省略 `subMenu` 鍵**（不輸出空陣列）；僅容器節點（有子項）含 `subMenu`，對齊前端 optional `subMenu?`。
- **FR-004**: 回應 MUST 僅包含 `text`、`value`、`subMenu` 三種鍵，移除其餘不必要欄位（如 id、icon、顯示順序、啟用狀態）。
- **FR-005**: `value` MUST 對應後端選單節點之既有識別鍵（SPEC-007 中存於選單資料表 `Route` 之前端 value）。
- **FR-006**: `subMenu` 陣列內節點順序 MUST 依後端既有顯示順序輸出（不因移除順序欄位而失序）。
- **FR-007**: 本功能 MUST NOT 變更資料表結構（不改 schema、不新增欄位、不產生 migration）；僅改變對外回應之塑形與欄位命名。
- **FR-008**: 回傳範圍（哪些節點）MUST 沿用既有「依角色可見性決定可見選單」之規則，本功能不改變可見性判定邏輯，僅改變輸出欄位形狀。
- **FR-009**: 本功能採「改既有 `/me/menus` 端點」（決策 A）：MUST 將其回應改為 `{text, value, subMenu}` 單一形狀，並 MUST 一併更新受影響之後端功能測試與契約快照（Verify）使其通過；既有前端（005）之調整以 API 契約交付前端團隊，不在本功能之後端程式範圍。
- **FR-010**: 欄位對齊 MUST 僅套用於 `/me/menus`；`/menus`（選單管理頁之選單樹端點）MUST 維持既有完整欄位（含 id、route、icon、顯示順序、啟用狀態）不變。故 `/me/menus` MUST 使用**專屬的精簡回應型別**，不再與 `/menus` 共用同一回應型別。

### 成功準則（Success Criteria）

- **SC-001**: 選單 API 回應可**直接**反序列化為前端 `MenuItem[]`（`{text, value, subMenu?}`），前端零欄位轉換。
- **SC-002**: 回應中每個節點的欄位鍵集合恰為 `{text, value, subMenu?}`，不含任何其他鍵（100% 精簡）。
- **SC-003**: 回應之節點順序與後端既有顯示順序 100% 一致。
- **SC-004**: 資料表零變更（無新增 migration）。

### 邊界情境（Edge Cases）

- **節點順序**：即使不回傳「顯示順序」欄位，`subMenu` 陣列中的節點順序仍 MUST 依後端既有顯示順序輸出（順序本身即語意）。
- **容器 vs 葉**：容器（有子節點）以 `subMenu` 呈現子項；葉節點不含 `subMenu`。
- **空選單**：使用者無任何可見選單時，回傳空陣列（`[]`），而非錯誤。
- **既有消費者衝擊（決策 A：只改 `/me/menus`）**：改變 `/me/menus` 欄位形狀會影響其消費者（005 側邊選單）、其功能測試與契約快照。本功能 MUST 一併更新受影響之後端功能測試與 Verify 契約快照；既有前端之對應調整以 API 契約交付前端團隊（後端限定工作流）。`/menus`（選單管理頁）不受影響。
- **兩端點型別分離**：原本 `/me/menus` 與 `/menus` 共用 `MenuTreeDto`；本功能後兩者回應型別分離——`/me/menus` 用精簡型別、`/menus` 續用完整型別。

### 關鍵實體（Key Entities）

- **選單節點回應（Menu node response）**：對外回傳的單一選單節點。關鍵屬性：`text`（顯示名稱）、`value`（識別鍵）、`subMenu`（子節點陣列，選填）。與前端 `MenuItem` 型別一一對應。
- **對齊來源（Alignment source）**：前端 `CHB.Web/src/composables/useMenu.ts` 之 `MenuItem` 介面（`{ text, value, subMenu? }`）為欄位形狀的權威來源（截圖所示）。

### 假設（Assumptions）

- 「後端傳前端」指的是取得使用者可見選單的既有 API（`/me/menus` 一類）；本功能改變其回應欄位形狀，可見性判定沿用既有規則。
- `value` 沿用 SPEC-007 之決策（前端 value 存於後端選單 `Route`）；本功能將該值以 `value` 之名對外輸出。
- 「不必要欄位」指目前回應中前端 accordionMenu 型別未涵蓋者：節點 id、icon、displayOrder、isEnabled。
- 資料表與可見性資料（SPEC-007 所種）維持不變。

## Acceptance Criteria（驗收條件）

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、SC-001、FR-001 | 每個節點含 `text`、`value`，回應可直接反序列化為 `{text, value, subMenu?}` | `backend/tests/TcmClinic.Api.FunctionalTests/NavigationEndpointsTests.cs` › `My_menu_shows_granted_leaf_with_ancestor_and_hides_others`；`backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs` › `Seeded_admin_sees_menus_and_can_access` |
| AC-2 | US1 情境 2、FR-002 | 子節點以 `subMenu` 遞迴巢狀 | `NavigationEndpointsTests` › `My_menu_shows_granted_leaf_with_ancestor_and_hides_others`；`backend/tests/TcmClinic.Api.FunctionalTests/Contracts/MyMenuContractTests.cs` › `My_menu_response_contract` |
| AC-3 | US1 情境 3、FR-003 | 葉節點完全省略 `subMenu` 鍵 | `NavigationEndpointsTests` › `My_menu_nodes_only_expose_text_value_subMenu`；`MyMenuContractTests` › `My_menu_response_contract` |
| AC-4 | US2 情境 1、SC-002、FR-004 | 節點鍵集合恰為 `{text, value}`（葉）或 `{text, value, subMenu}`（容器） | `NavigationEndpointsTests` › `My_menu_nodes_only_expose_text_value_subMenu`；`MyMenuContractTests` › `My_menu_response_contract` |
| AC-5 | US2 情境 2 | 不含 `id`／`icon`／`displayOrder`／`isEnabled`／`route`／`name`／`children` | `NavigationEndpointsTests` › `My_menu_nodes_only_expose_text_value_subMenu` |
| AC-6 | FR-005 | `value` 等於後端選單 `Route` | `NavigationEndpointsTests` › `My_menu_shows_granted_leaf_with_ancestor_and_hides_others`；`AuthEndpointsTests` › `Seeded_admin_sees_menus_and_can_access` |
| AC-7 | SC-003、FR-006 | `subMenu` 陣列順序＝後端顯示順序 | `MyMenuContractTests` › `My_menu_response_contract`（快照鎖定節點順序） |
| AC-8 | FR-008 | 可見性判定不變：已授權葉與祖先容器顯示、未授權項隱藏 | `NavigationEndpointsTests` › `My_menu_shows_granted_leaf_with_ancestor_and_hides_others` |
| AC-9 | FR-010 | `/menus` 仍回完整欄位（`id`／`name`／`route`／`isEnabled`／`children` 等） | `backend/tests/TcmClinic.Api.FunctionalTests/MenuEndpointsTests.cs` › `Create_then_appears_in_tree`（以完整欄位型別反序列化 `/menus`）；手動：Test Plan 手動走查第 3 步 |
| AC-10 | SC-004、FR-007 | 無 schema 變更、無新增 migration | 手動：Code Review 確認實作提交 `7b54775` 未含任何 Migrations 檔（Test Plan 手動走查第 4 步） |
| AC-11 | Edge Case（空選單）、FR-009 | 無任何可見選單時回 `[]`；受影響功能測試與契約快照已一併更新 | 空選單：手動：【待確認】無自動測試（原任務 T002 將此斷言列為選用，未實作），見 Test Plan 手動走查第 2 步；測試與快照更新：`MyMenuContractTests` › `My_menu_response_contract`、`AuthEndpointsTests` › `Seeded_admin_sees_menus_and_can_access` |

> AC-7 的契約快照自 SPEC-009 起以 SUPERADMIN 全部可見選單為內容（整棵 CHB 樹＋`/admin/menus`），並隨 SPEC-007 2026-10-01／10-02 的選單調整更新；本規格鎖定的是節點形狀與順序，快照內容隨種子演進屬預期。

## Technical Design（技術設計）

### 技術脈絡

- 語言／框架：C# / .NET（沿用 repo 現有 target）。
- 相依：FastEndpoints、Mediator、System.Text.Json（序列化省略空 `subMenu`）；無新增第三方套件。
- 儲存：無變更（不觸及資料庫）。
- 測試：xUnit + Testcontainers（Functional：`/me/menus` 端到端）；Verify（契約快照）。
- 專案類型：Web（後端限定；前端由另一團隊維護，本功能以 API 契約交付）。
- 效能：讀取端點沿用 ≤300ms p95；本功能僅換對映，無額外查詢。
- 限制：僅改 `/me/menus` 回應形狀，`/menus` 維持不變；葉節點 MUST 省略 `subMenu` 鍵（需序列化「寫入 null 時略過」）；`subMenu` 節點順序 MUST 依既有顯示順序；不改 schema、無 migration；可見性判定邏輯不變。
- 規模：單一端點回應塑形變更 + 一個新 DTO + 更新功能測試 + 新增一個契約快照 + 契約交付文件。

實作摘要：將 `GET /me/menus` 的回應由既有完整 `MenuTreeDto`（`Id/Name/Route/Icon/DisplayOrder/IsEnabled/Children`）改為專屬精簡型別 `MyMenuNodeDto`：`text`（=Name）、`value`（=Route）、`subMenu`（=子節點；葉節點省略此鍵）。沿用 `GetMyMenuHandler` 的授權／建樹邏輯，只換輸出對映；`subMenu` 順序沿用 `AllMenuItemsOrderedSpec`／`MenuTree.BuildVisible`。

### 受影響元件

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.UseCases/Navigation/MyMenuNodeDto.cs` | 新增：精簡回應型別 `{ Text, Value, SubMenu? }`，`SubMenu` 標註 `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` |
| `backend/src/TcmClinic.Api.UseCases/Navigation/GetMyMenu.cs` | 修改：Query／Handler 回應型別改為 `MyMenuNodeDto`；對映省略空 `subMenu` |
| `backend/src/TcmClinic.Api.Web/Endpoints/Navigation/GetMyMenuEndpoint.cs` | 修改：回應泛型型別改為 `IReadOnlyList<MyMenuNodeDto>`；更新 Summary（zh-TW） |
| `backend/tests/TcmClinic.Api.FunctionalTests/NavigationEndpointsTests.cs` | 修改：預期新形狀並保留可見性斷言；新增鍵集合測試 |
| `backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs` | 修改：`/me/menus` 本地記錄改為 `{ text, value, subMenu? }`，改讀 `Value` |
| `backend/tests/TcmClinic.Api.FunctionalTests/Contracts/MyMenuContractTests.cs` | 新增：`/me/menus` 回應之 Verify 契約快照（`MyMenuContractTests.My_menu_response_contract.verified.txt`） |

明確不動：`backend/src/TcmClinic.Api.UseCases/Menus/MenuTreeDto.cs`、`backend/src/TcmClinic.Api.Web/Endpoints/Menus/GetMenuTreeEndpoint.cs`（`/menus` 續用完整型別）、資料庫與可見性邏輯、前端 `CHB.Web/src/composables/useMenu.ts`。

### 資料模型

本功能**不新增／不變更資料庫實體**。僅新增一個**對外回應 DTO**並調整 `/me/menus` 之輸出對映。

#### 新增回應型別：`MyMenuNodeDto`（UseCases/Navigation）

| 屬性 | 型別 | JSON 鍵 | 來源 | 說明 |
|------|------|---------|------|------|
| Text | string | `text` | `MenuItem.Name` | 顯示名稱 |
| Value | string | `value` | `MenuItem.Route` | 前端識別鍵（SPEC-007 存入之 value，含容器） |
| SubMenu | `IReadOnlyList<MyMenuNodeDto>?` | `subMenu` | 子節點遞迴 | **無子節點時為 null 並省略此鍵**；容器為非空陣列 |

- 序列化：`SubMenu` 標註 `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` → 葉節點不輸出 `subMenu`。
- 命名：PascalCase 屬性經預設 camelCase 策略輸出為 `text`/`value`/`subMenu`。
- 移除欄位：`Id`、`Icon`、`DisplayOrder`、`IsEnabled` 不在此型別（僅 `/menus` 之 `MenuTreeDto` 保留）。

#### 對映規則（取代 `GetMyMenuHandler.Map`）

```
Map(nodes):
  for each node（已依既有顯示順序排列）:
    children = Map(node.Children)
    yield MyMenuNodeDto(
      Text  = node.Item.Name.Value,
      Value = node.Item.Route,               // 007：容器/葉皆有 value
      SubMenu = children.Count > 0 ? children : null)   // 空則 null → 省略鍵
```

- 順序：沿用 `AllMenuItemsOrderedSpec` / `MenuTree.BuildVisible`；對映不重排。
- 可見性：不變（角色可見葉 → 補祖先容器 → 過濾停用），僅換最終對映型別。

#### 不變動

- `MenuTreeDto`（UseCases/Menus）與 `/menus`（`GetMenuTreeEndpoint`）維持完整欄位。
- 資料表、可見性資料（SPEC-007 所種）、授權政策皆不變。

#### 驗證規則（對應 FR／SC）

| 規則 | 來源 | 驗證點 |
|------|------|--------|
| 每節點含 `text`/`value` | FR-001, SC-001 | 回應結構 |
| 子節點以 `subMenu` 巢狀 | FR-002 | 回應結構 |
| 葉節點省略 `subMenu` 鍵 | FR-003, SC-002 | 回應鍵集合 |
| 節點鍵僅 `{text,value,subMenu?}` | FR-004, SC-002 | 回應鍵集合 |
| `value` = 後端 Route（007） | FR-005 | 值比對 |
| `subMenu` 順序＝顯示順序 | FR-006, SC-003 | 順序比對 |
| 不改 schema | FR-007, SC-004 | 無 migration |
| 可見性判定不變 | FR-008 | 沿用既有測試情境 |
| `/menus` 不受影響 | FR-010 | `/menus` 仍回完整欄位 |

無狀態機。

### API 契約

#### 原 contracts/me-menus.response.md

> 交付對象：前端團隊（本功能後端限定；此契約為前端調整之依據）。小節標題沿用原契約文件。

##### 端點

- **Method / Path**：`GET /me/menus`
- **授權**：任何已登入使用者（未登入 → 401）。
- **語意**：回傳目前使用者依角色可見的側邊選單樹（沿用既有可見性規則：角色可見葉 → 補祖先容器 → 排除停用）。

##### 回應（200）

對齊前端 `CHB.Web/src/composables/useMenu.ts` 之 `MenuItem` 型別：

```ts
interface MenuItem {
  text: string          // 顯示名稱
  value: string         // 識別鍵（= 後端選單 Route）
  subMenu?: MenuItem[]   // 子節點；葉節點省略此鍵
}
// 回應為 MenuItem[]（頂層節點陣列）
```

**規則**

- 每個節點僅含 `text`、`value`、以及（若為容器）`subMenu`。
- **葉節點不含 `subMenu` 鍵**（非空陣列、非 `null`）。
- `subMenu` 陣列順序＝後端顯示順序（順序即語意，不另附排序欄位）。
- 無任何可見選單時回傳 `[]`。
- **不再包含**舊欄位：`id`、`route`、`icon`、`displayOrder`、`isEnabled`、`children`。

**範例**

```json
[
  { "text": "首頁", "value": "home" },
  {
    "text": "進口業務",
    "value": "import",
    "subMenu": [
      {
        "text": "受理作業",
        "value": "importAccept",
        "subMenu": [
          { "text": "受理開狀申請書", "value": "importAcceptLcApp" },
          { "text": "受理修狀申請書", "value": "importAcceptAmendApp" }
        ]
      }
    ]
  }
]
```

##### 錯誤

| 狀態 | 情境 |
|------|------|
| 401 | 未登入 |

##### 相容性備註

- 本契約**取代** `/me/menus` 舊回應（`{ id, name, route, icon, displayOrder, isEnabled, children }`）。
- `GET /menus`（選單管理頁）**不變**，仍回完整欄位；勿與本端點混用。

### 設計決策（原 research）

> 決策編號沿用原設計研究文件（D1–D8）。

#### D1 只改 `/me/menus`，另立精簡回應型別（Clarifications 第 2 題＝A）
- **決定**：新增 `MyMenuNodeDto { string Text, string Value, IReadOnlyList<MyMenuNodeDto>? SubMenu }`，`GetMyMenuQuery`／`GetMyMenuHandler` 回應型別改用之；`/menus`（`GetMenuTreeQuery` → `MenuTreeDto`）不動。
- **理由**：`/menus` 供選單管理頁編輯，需保留 id/route/icon/順序/啟用；分離型別可精簡 `/me/menus` 而不傷管理頁。
- **替代方案**：改共用 `MenuTreeDto`（否決——會拔掉管理頁必要欄位）；新增獨立端點（否決——釐清定為改既有端點）。

#### D2 欄位對映
- **決定**：`Text ← MenuItem.Name`、`Value ← MenuItem.Route`（SPEC-007 存入之前端 value，含容器）、`SubMenu ← 子節點遞迴`。丟棄 `Id/Icon/DisplayOrder/IsEnabled`。
- **理由**：對齊前端 `MenuItem { text, value, subMenu? }`；`value` 沿用 SPEC-007 決策。
- **替代方案**：原文件未列。

#### D3 葉節點省略 `subMenu` 鍵（Clarifications 第 3 題＝A）
- **決定**：`SubMenu` 為可空清單；**無子節點時設為 `null`**，並於 DTO 屬性標註 `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`，使葉節點序列化時**完全不輸出 `subMenu` 鍵**。容器節點（有子項）輸出非空 `subMenu` 陣列。
- **理由**：專案 Web 未設全域 `DefaultIgnoreCondition`（已確認無自訂 JSON 設定），故需以屬性層級略過 null；與前端原始 `accordionMenu`（葉節點無 `subMenu` 鍵）一致。
- **替代方案**：輸出 `subMenu: []`（釐清否決）；設全域 WhenWritingNull（否決——影響全站其他回應，範圍過大）。

#### D4 欄位命名（JSON 大小寫）
- **決定**：C# 屬性 `Text/Value/SubMenu` → JSON `text/value/subMenu`。
- **理由**：Web 無自訂命名策略，沿用 FastEndpoints 預設 System.Text.Json（camelCase、反序列化不分大小寫），故 PascalCase 屬性自動輸出為 camelCase，正好對齊前端。
- **替代方案**：原文件未列。

#### D5 節點順序
- **決定**：沿用既有 `AllMenuItemsOrderedSpec` 與 `MenuTree.BuildVisible` 決定之順序；對映時保持清單順序。
- **理由**：即使不輸出 `displayOrder` 欄位，`subMenu` 陣列順序即語意（FR-006）。不改排序來源。
- **替代方案**：原文件未列。

#### D6 可見性與範圍
- **決定**：不改 `GetMyMenuHandler` 之授權／建樹（角色可見葉→補祖先容器→過濾停用）；只換最終 `Map`。
- **理由**：FR-008——僅改輸出形狀，不動可見性判定。
- **替代方案**：原文件未列。

#### D7 受影響測試與契約
- **決定**：
  - 更新 `NavigationEndpointsTests`（GET /me/menus）之預期為新形狀 `{text, value, subMenu}`（葉無 `subMenu`）。
  - 新增 `Contracts/MyMenuContractTests`（Verify 快照）鎖定 `/me/menus` 新回應（現況選單無契約快照，屬新增；符合 Principle XV）。
- **理由**：FR-009 要求一併更新受影響之功能測試與契約快照；選單原無快照，故為新增。
- **替代方案**：原文件未列。

#### D8 前端交付
- **決定**：以「API 契約」一節（原 me-menus 回應契約文件）描述新回應形狀，交付前端團隊（後端限定工作流）；本功能不改前端程式。
- **理由**：憲章「後端限定工作流」——前端變更以契約交付。
- **替代方案**：原文件未列。

未解決事項：無（規格無殘留待釐清標記）。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)（以下原則編號為 2026-09-07 當時憲章版本）：無違規，Complexity Tracking 留空。I Clean Architecture——新 DTO 置於 UseCases（Navigation）、Handler 在 UseCases、Endpoint 在 Web，依賴向內；III CQRS & Mediator——僅改 `GetMyMenuQuery` 回應型別與 `GetMyMenuHandler` 對映，`/menus` 之 Query 不動；VIII／X zh-TW 與註解風格——註解、端點 metadata、測試 DisplayName 皆 zh-TW 並附 Test-intent；XI Schema Conventions——無資料庫變更、無 migration；XII Test-First——先改／寫失敗的 `/me/menus` 功能測試與契約快照再改實作；XIII UX Consistency——有意調整 `/me/menus` 回應形狀以對齊前端，錯誤處理／狀態碼不變（仍 200/401）；XIV Performance——同一查詢僅換對映，無 N+1；XV Testing Guardrails——新增 `/me/menus` Verify 契約快照；XVI Dependency Governance——無新增依賴；後端限定工作流——不改前端程式，新契約交付前端團隊。

## Test Plan（測試計畫）

### 自動化測試

- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/NavigationEndpointsTests.cs`：新形狀、可見性過濾、鍵集合（AC-1～AC-6、AC-8）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs`：種子 admin 以 `value` 讀取可見選單（AC-1、AC-6、AC-11）。
- [x] 契約快照 `backend/tests/TcmClinic.Api.FunctionalTests/Contracts/MyMenuContractTests.cs`：`/me/menus` 回應形狀與順序（AC-2～AC-4、AC-7、AC-11）。
- [x] 既有測試 `backend/tests/TcmClinic.Api.FunctionalTests/MenuEndpointsTests.cs`：`/menus` 完整欄位不受影響（AC-9）。

執行：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests -- -class "TcmClinic.Api.FunctionalTests.NavigationEndpointsTests" -class "TcmClinic.Api.FunctionalTests.Contracts.MyMenuContractTests" -class "TcmClinic.Api.FunctionalTests.AuthEndpointsTests" -class "TcmClinic.Api.FunctionalTests.MenuEndpointsTests"
```

預期：全綠；`/me/menus` 回應可直接反序列化為前端 `MenuItem[]`。Verify 契約快照首次執行會產生 `*.received`，確認新形狀後接受 `.verified` 快照。

### 手動走查

前置：後端可建置並連 SQL Server；Functional／Contract 測試需 Docker（Testcontainers）。

1. 登入後呼叫 `GET /me/menus`：每個節點含 `text`、`value`，容器以 `subMenu` 巢狀；節點鍵集合恰為 `{text, value, subMenu?}`，葉節點無 `subMenu` 鍵，無 `id/icon/displayOrder/isEnabled`；`subMenu` 順序＝後端顯示順序；`value` 等於後端選單 `Route`。
2. 以無任何可見選單的使用者登入呼叫 `GET /me/menus`：應回 `[]`（非錯誤）。
3. 呼叫 `GET /menus`（管理頁）：仍回完整欄位（`id/name/route/icon/displayOrder/isEnabled/children`），不受影響。
4. 檢查實作提交未新增任何 migration 檔（schema 未變）。

## Dependencies（相依）

- SPEC-007 CHB選單與角色種子：選單樹與角色可見性資料（`value` 存於 `Route`）。見 CHB選單與角色種子。
- SPEC-005 選單導向授權模型：既有「依角色可見性回傳選單」之機制（本功能改其輸出欄位形狀）。見 [選單導向授權模型](../選單導向授權模型/spec.md)。
- 對齊來源：`CHB.Web/src/composables/useMenu.ts`（`MenuItem` 型別）。
- 下游：SPEC-009 開發模式可見全部選單沿用本規格回應契約，見 [開發模式可見全部選單](../開發模式可見全部選單/spec.md)。

## Risks（風險）

- `/me/menus` 回應形狀為破壞性變更：既有前端（005 側邊選單）須依「API 契約」一節調整，由前端團隊負責。
- 契約快照內容取決於種子 admin 的可見選單；選單種子異動（如 SPEC-007、SPEC-009）會使快照需重新核可，須人工確認差異僅來自選單內容而非節點形狀。
- 空選單回 `[]` 目前無自動測試守護（AC-11）。

## 實作紀錄（Archive）

- **狀態**：已實作並合併，所有任務完成（T001–T013）。
- **主要提交**：`0a9db3f` 規格與釐清 → `2348df4` 計畫與任務（含一致性分析修正 C1–C4）→ `7b54775` `/me/menus` 回應欄位對齊前端 → `3487091` 合併（2026-09-07）；`7021ef6` 規格目錄改依選單分層。
- **審查紀錄**：規格品質檢查（原 checklists/requirements.md）全數通過；範圍決策（採 A：改既有 `/me/menus`）於 2026-09-07 拍板後進入計畫。一致性分析 C1（`AuthEndpointsTests` 亦為消費者）、C2（保留可見性斷言）、C3（契約快照用最小 fixture）已納入任務；C4（空選單 `[]` 斷言）列為選用，未實作。
- **測試數**：全套 132 測試綠（規格索引記錄）。
- **原 Spec Kit 產出**：plan、research、data-model、quickstart、tasks、checklists、contracts 已併入本檔後移除；原始檔可由 `git show 7021ef6:"specs/其他/選單API欄位對齊前端/tasks.md"` 等方式追溯。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：沒有單獨提交紅燈 commit，無紀錄可補。
