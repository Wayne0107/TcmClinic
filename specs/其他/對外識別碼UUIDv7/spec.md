# [SPEC-064] Feature: 對外識別碼 UUIDv7

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-064 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 無（跨選單基礎規格） |
| 建立日期 | 2026-10-08 |
| 核准日期 | 2026-10-08 |
| 核准人 | Wayne（授權 AI 直接決定並修改） |
| 最後更新 | 2026-10-08（實作完成並通過驗證） |
| 原分支 | `feature/064-public-ids` |
| 相關規格 | 修改 [SPEC-002 後台選單與角色權限管理](../後台選單與角色權限管理/spec.md)、[SPEC-004 使用者管理與登入](../使用者管理與登入/spec.md)、SPEC-011 受理開狀申請書、[SPEC-012 角色權限維護](../../管理業務/系統管理作業/角色權限維護/spec.md)、SPEC-059 企業資料維護 的 API 識別碼；依 [ADR-010](../架構決策/ADR-010-identity-primary-keys.md)（內部主鍵 IDENTITY） |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

## Overview（概述）

API 不再公開資料表的整數主鍵。凡是識別碼會出現在 API（路徑、回應、請求內容）的實體，另加一個 UUIDv7 的對外識別碼 `PublicId`，API 一律以它識別資料；資料表內部的主鍵、外鍵與關聯仍用 int IDENTITY（ADR-010）。

## Motivation（動機）

2026-10-08 開發規範裁決第 2 題採 UDS「內部 ID 用自動遞增；分散式或對外 ID 用 UUIDv7」。DEF-012 已完成內部主鍵的部分；本系統的 API 會提供給前端，識別碼屬於對外使用，需補上 UUIDv7（延後事項 DEF-020）。

公開連號整數主鍵會讓呼叫端從 `/users/5`、`/users/6` 推知資料筆數與建立順序，也讓逐號試探（IDOR 枚舉）更容易。UUIDv7 不可推測，又因前段為時間戳而大致遞增，產生時不需查資料庫。

## Clarifications（釐清紀錄）

### Session 2026-10-08

背景：使用者表示 API 會提供給前端，應補上 UUIDv7，並授權由 AI 直接決定以下細節與修改程式、規格；前端由使用者另行說明。

- Q: 主鍵直接改成 UUID，還是另加欄位？ → A: **另加 `PublicId` 欄位**。主鍵維持 int IDENTITY（剛完成的 ADR-010），外鍵、關聯、索引不受影響；只有對外介面改用 `PublicId`。直接改主鍵要連同所有外鍵一起改，成本大很多、好處相同。
- Q: 哪些實體要加？ → A: 識別碼會出現在 API 的 6 種：使用者（`SysUser`）、角色（`SysRole`）、選單（`SysTask`）、開狀申請（`LcApplication`）、企業（`Enterprise`）、退件原因（`RefuseReason`）。分行與系統參數在 API 上以代碼識別，不加。
- Q: JSON 欄位名稱要不要改？ → A: **不改**。仍是 `id`、`roleIds`、`parentId`、`orderedIds`、`grantedMenuItemIds`、`refuseReasonId`，值由整數改為 UUID 字串；前端只需調整型別。
- Q: 型別？ → A: 每種實體一個 Vogen 強型別（如 `UserPublicId`，`Guid` 底層），避免把使用者識別碼誤傳成角色識別碼（憲章原則 IV）；端點與命令沿用原本以原始型別傳遞識別碼的做法（命令收 `Guid`），由用例以 `XxxPublicId.TryFrom` 轉為強型別，`Guid.Empty` 視同查無資料。
- Q: 如何產生？既有資料怎麼辦？ → A: 實體建構時以 `Guid.CreateVersion7()` 產生，不需查資料庫。SQL Server 沒有內建 UUIDv7，migration 為既有資料補值時用 `NEWID()`（v4）；系統尚未上線，既有資料只有開發種子，可接受。
- Q: 路徑格式不是 UUID 時？ → A: 路由約束 `{id:guid}`，格式不符視同不存在的路由，回 404 ProblemDetails（DEF-011 的錯誤格式）。請求內容中的識別碼若查無對應資料，沿用各端點原本對「不存在的識別碼」的處理（400 或 404）。

## Requirements（需求）

### Requirement: US1 API 以 UUIDv7 識別資料（P1）

The system SHALL 讓所有以識別碼操作資料的 API，在路徑、回應與請求內容中一律使用該實體的對外識別碼（UUIDv7），不再出現整數主鍵。

前端與其他呼叫端拿到的識別碼無法推知資料筆數或順序，也無法以連號試探。涵蓋 FR-001～FR-005。

#### Scenario: AC-1 回應中的識別碼為 UUIDv7
- **GIVEN** 已有使用者、角色、選單、開狀申請、企業、退件原因資料
- **WHEN** 呼叫各清單或明細 API
- **THEN** 回應中的 `id` 與參照欄位皆為 UUID 字串，版本為 7（新建資料），不出現整數主鍵

#### Scenario: AC-2 以對外識別碼操作資料
- **GIVEN** 由清單 API 取得某筆資料的 `id`
- **WHEN** 以該值呼叫明細、修改、刪除或狀態轉換 API
- **THEN** 操作成功，行為與原本以整數主鍵操作時相同

#### Scenario: AC-3 整數或格式錯誤的識別碼不被接受
- **GIVEN** 某筆資料
- **WHEN** 以其整數主鍵或非 UUID 字串呼叫路徑含 `{id}` 的 API
- **THEN** 回 404 ProblemDetails，不洩漏資料是否存在

#### Scenario: AC-4 請求內容中的參照
- **GIVEN** 角色、選單、退件原因的對外識別碼
- **WHEN** 新增或修改使用者時帶 `roleIds`、設定角色可見性時帶 `grantedMenuItemIds`、新增或排序選單時帶 `parentId`／`orderedIds`、退件時帶 `refuseReasonId`
- **THEN** 依對外識別碼找到對應資料，行為與原本相同；查無對應資料時照原本規則拒絕

### 功能需求條文（Functional Requirements）

- **FR-001**: 系統 MUST 為使用者、角色、選單、開狀申請、企業、退件原因各加一個對外識別碼 `PublicId`（UUID，NOT NULL，唯一）。
- **FR-002**: 新建實體的 `PublicId` MUST 為 UUIDv7，於實體建構時產生，建立後不可變更。
- **FR-003**: API 的路徑參數、回應與請求內容中，上述實體的識別碼 MUST 一律為 `PublicId`；MUST NOT 出現整數主鍵。
- **FR-004**: 路徑中的識別碼 MUST 以 `{id:guid}` 約束；格式不符或查無資料時回 404 ProblemDetails。
- **FR-005**: 資料表內部的主鍵、外鍵與關聯 MUST 維持 int IDENTITY（ADR-010），不因本規格改變。

### 邊界情境（Edge Cases）

- 已軟刪除的資料：以其 `PublicId` 查詢時照原本規則視為不存在（404）。
- 同一請求中重複的參照識別碼（如 `roleIds` 重複）：照原本以整數主鍵時的去重規則處理。
- 既有開發資料的 `PublicId` 為 v4（migration 以 `NEWID()` 補值），不影響唯一性與查詢。

### 關鍵實體（Key Entities）

- **對外識別碼（PublicId）**：實體對外的唯一識別，UUIDv7；與內部主鍵一對一、不可變。

### 假設（Assumptions）

- 前端由另一團隊維護，識別碼型別的調整由使用者向前端團隊說明；本規格只涵蓋後端。

## Acceptance Criteria（驗收條件）

| AC | 來源 | 驗收條件 | 對應測試 |
|----|------|---------|---------|
| AC-1 | US1、FR-001～FR-003 | 6 種實體的清單或明細回應中，`id` 與參照欄位皆為 UUIDv7 字串 | `TcmClinic.Api.FunctionalTests/PublicIdTests.cs` › `Responses_use_uuid_v7_ids` |
| AC-2 | US1、FR-003 | 以清單取得的 `id` 呼叫明細、修改、刪除、受理等 API 成功 | 各功能測試（改用 `PublicId` 後全數通過）；`PublicIdTests` › `Operations_accept_public_id` |
| AC-3 | US1、FR-004 | 以整數主鍵或非 UUID 字串呼叫 `{id}` 路徑回 404 ProblemDetails | `PublicIdTests` › `Integer_or_malformed_id_returns_404` |
| AC-4 | US1、FR-003 | 請求內容的 `roleIds`、`grantedMenuItemIds`、`parentId`、`orderedIds`、`refuseReasonId` 以對外識別碼對應 | 各功能測試（使用者、角色可見性、選單、退件） |
| AC-5 | FR-001、FR-002、FR-005 | 6 張表有 `PublicId uniqueidentifier NOT NULL` 與唯一索引、有中文說明；新建實體的 `PublicId` 為 v7；主鍵仍為 IDENTITY | `TcmClinic.Api.IntegrationTests/Data/PublicIdSchemaTests.cs` |

## Technical Design（技術設計）

### 技術脈絡

- .NET 10 內建 `Guid.CreateVersion7()`，不新增套件。
- 端點路由約束 `{id:guid}`；FastEndpoints 以 `Route<Guid>("id")` 取值。

### 受影響元件

| 層 | 元件 |
|----|------|
| Core | 6 個 Vogen 型別 `UserPublicId`、`RolePublicId`、`MenuItemPublicId`、`LcApplicationPublicId`、`EnterprisePublicId`、`RefuseReasonPublicId`；6 個實體加 `PublicId` 屬性；`XxxByPublicIdSpec` |
| Infrastructure | 6 個 EF 設定加 `PublicId`（轉換、必填、說明、唯一索引）；migration `PublicIds` |
| UseCases | 以識別碼查詢的命令與查詢改收 `Guid`，以 `PublicId` 查詢；回應 DTO 的識別碼改為 `Guid` |
| Web | 路徑改 `{id:guid}`、請求與回應的識別碼改為 `Guid` |

### 資料模型

6 張表各新增：

| 欄位 | 型別 | 說明 |
|------|------|------|
| `PublicId` | `uniqueidentifier`，NOT NULL | 對外識別碼（UUIDv7，API 一律使用此值，不公開整數主鍵） |
| `IX_<表>_PublicId` | 唯一索引 | 以對外識別碼查詢 |

migration `PublicIds`：以預設值 `NEWID()` 加入 NOT NULL 欄位（既有資料隨之補值），再移除預設值並建唯一索引；之後新增的資料一律由應用程式寫入 UUIDv7。

### API 契約

識別碼型別由 `int` 改為 UUID 字串（JSON 欄位名稱不變）：

| 端點 | 變動 |
|------|------|
| `GET /users`、`POST /users`、`PUT /users/{id}`、`DELETE /users/{id}`、`PUT /users/{id}/password` | 路徑 `{id:guid}`；回應 `id`、請求 `roleIds` 為 UUID |
| `GET /roles`、`GET /roles/maintenance`、`POST /roles`、`PUT /roles/{id}`、`DELETE /roles/{id}` | 路徑 `{id:guid}`；回應 `id` 為 UUID |
| `GET /roles/{id}/menu-visibility`、`PUT /roles/{id}/menu-visibility` | 路徑 `{id:guid}`；節點 `id`、請求 `grantedMenuItemIds` 為 UUID |
| `GET /menus`、`POST /menus`、`PUT /menus/{id}`、`DELETE /menus/{id}`、`PATCH /menus/{id}/enabled`、`PUT /menus/reorder` | 路徑 `{id:guid}`；節點 `id`、請求 `parentId`、`orderedIds` 為 UUID |
| `GET /lc-applications`、`GET /lc-applications/{id}`、`GET /lc-applications/{id}/pdf`、`POST /lc-applications/{id}/accept`、`POST /lc-applications/{id}/reject` | 路徑 `{id:guid}`；回應 `id`、請求 `refuseReasonId` 為 UUID |
| `GET /refuse-reasons` | 回應 `id` 為 UUID |
| `GET /enterprises`、`GET /enterprises/{id}`、`POST /enterprises`、`PUT /enterprises/{id}`、`DELETE /enterprises/{id}` | 路徑由 `{id:int}` 改 `{id:guid}`；回應 `id` 為 UUID |

### 設計決策（原 research）

#### R1 另加 PublicId，主鍵不變
- **決定**：主鍵維持 int IDENTITY，另加 `PublicId`。
- **理由**：外鍵、索引、ADR-010 的成果都不受影響；對外需求只在 API 邊界。
- **替代方案**：主鍵改 UUID（需改所有外鍵與關聯，並放棄 ADR-010）；對整數主鍵加密或雜湊後公開（可逆編碼不算真正隨機，且演算法外洩即失效）。

#### R2 每種實體一個強型別
- **決定**：6 個 Vogen `Guid` 型別。
- **理由**：憲章原則 IV，編譯期防止誤用；與既有 `XxxId` 型別一致。

#### R3 JSON 欄位名稱不變
- **決定**：沿用 `id` 等名稱，只改型別。
- **理由**：前端調整範圍最小；欄位語意不變。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)：原則 I（安全）——不公開連號主鍵，降低枚舉風險；原則 IV（類型安全）——以強型別表達對外識別碼；原則 III（分層）——轉換在端點與用例邊界完成，領域與持久層不依賴 API 型別。PASS。

## Test Plan（測試計畫）

### 自動化測試

- 整合：`PublicIdSchemaTests`——6 張表欄位、唯一索引、說明；新建實體為 v7；主鍵仍為 IDENTITY。
- 功能：`PublicIdTests`——回應識別碼為 UUIDv7、以 `PublicId` 操作成功、整數或格式錯誤回 404。
- 既有功能測試全數改用 `PublicId` 後須通過（AC-2、AC-4）。

### 手動走查

- 前端串接時由前端團隊驗證；本規格不另做人工走查。

## Dependencies（相依）

- ADR-010（內部主鍵 IDENTITY）；ProblemDetails 錯誤格式（DEF-011）。

## Risks（風險）

- **前端契約變更**：所有使用上述識別碼的前端頁面都要調整型別；由使用者向前端團隊說明。
- 既有開發資料的 `PublicId` 為 v4；不影響功能。

## 實作紀錄（Archive）

- **狀態**：已實作並合併歸檔（2026-10-08），分支 `feature/064-public-ids`；延後事項 DEF-020 結案。
- **實作**：
  - Core：6 個 Vogen `Guid` 型別（`New()` 以 `Guid.CreateVersion7()` 產生、拒絕 `Guid.Empty`）；6 個實體加唯讀 `PublicId`，建構時產生；以識別碼查詢的規格改為 `XxxByPublicIdSpec`。
  - Infrastructure：6 個 EF 設定加 `PublicId`（`ValueGeneratedNever`、必填、中文說明、唯一索引 `IX_<表>_PublicId`）；migration `20261008084625_PublicIds`。
  - UseCases：命令與查詢改收 `Guid`，先以 `TryFrom` 轉強型別（`Guid.Empty` 視同查無資料），再以 `PublicId` 查詢；請求內容中的參照（`roleIds`、`grantedMenuItemIds`、`parentId`、`orderedIds`、`refuseReasonId`）先換成內部主鍵再沿用原本的規則；新增使用者、角色、選單回傳 `PublicId`；`MenuParent.ResolveAsync` 改以 `Guid?` 解析上層選單（`null` 為頂層）。
  - Web：路徑一律 `{id:guid}`（企業由 `{id:int}` 改）；請求與回應的識別碼改 `Guid`；Swagger 範例值集中在 `Endpoints/ExampleIds.cs`。
- **TDD 紅燈證據**（本機驗證，未提交）：先寫 `PublicIdTests` 與 `PublicIdSchemaTests`。紅燈時 `PublicIdSchemaTests` 因實體尚無 `PublicId` 無法編譯；`PublicIdTests` 的 AC-1 失敗（`id` 為 Number）、AC-3 失敗（`/roles/1/menu-visibility`、`/enterprises/1`、`/lc-applications/1` 回 200 或 403，不是 404）。AC-3 原本以 `GET /users/1` 測試，因該路徑沒有 GET 方法而回 405，改用實際存在的 `PUT /users/1`。
- **BDD**：本規格的 AC 為識別碼格式與資料表結構，不適合以 Gherkin 描述，照 docs/TESTING.md 以功能與整合測試表達。
- **既有測試調整**：功能測試的請求與回應型別改 `Guid`、輔助方法改回傳 `PublicId`；寫死的 `/roles/1` 改為固定的 UUID；Verify 快照的 `id` 由數字改為 `Guid_N`；結構測試的欄位數（`Enterprise` 14 欄）與插入語句補上 `PublicId`。
- **測試數**：建置 0 錯誤 0 警告；單元 313、整合 156、功能 317 全綠。
- **驗證（verify，2026-10-08）**：PASS。AC-1～AC-5 皆有程式與測試對應且通過；`UseCases`、`Web` 已無 `int` 型別的識別碼。
- **審查（code-review，2026-10-08）**：無 BLOCKING／IMPORTANT。依建議刪除未使用的 `RolesByPublicIdsSpec`、`MenuItemsByPublicIdsSpec`，修正 Clarifications 中識別碼轉型位置的描述，Swagger 路徑參數說明改為「對外識別碼（UUIDv7）」。
- **行為差異**：`PUT /menus/reorder` 帶查無資料的 `parentId` 且 `orderedIds` 為空時，原本回 200（不做事），現在回 400「父選單不存在」；`/lc-applications/{id}` 原本無型別約束，非數字回 400，現在格式不符回 404（FR-004）。
- **前端影響**：識別碼型別由數字改為字串，欄位名稱不變；契約變動見「API 契約」表，由使用者向前端團隊說明。
- **合併**：PR #9，squash commit `f3b3a12`（2026-10-08）；規格狀態改為 Archived。
