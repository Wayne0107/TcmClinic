# [SPEC-065] Feature: 版本號與建置識別

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-065 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 無（跨選單基礎規格） |
| 建立日期 | 2026-10-08 |
| 核准日期 | 2026-10-08 |
| 核准人 | Wayne（授權 AI 直接決定並修改） |
| 最後更新 | 2026-10-08（實作完成、通過驗證並合併歸檔） |
| 原分支 | `feature/065-version-endpoint` |
| 相關規格 | 無直接相依；落實 `docs/INFRASTRUCTURE.md` §4 的版本號與建置識別規範（延後事項 DEF-016） |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

## Overview（概述）

後端採 SemVer 版本號，版本號只設定在一處；建置時把 git commit sha（所有建置）與建置時間（Release 建置）寫入組件。部署的服務提供 `GET /version`，回傳 `version`、`sha`、`buildTime`，發版後以此確認線上執行的就是發版的那個 commit。

## Motivation（動機）

2026-10-08 開發規範裁決改採 UDS 的版本規範（SemVer 與 `/version`）。目前組件版本是 SDK 預設的 `1.0.0`，部署後無法從外部確認線上跑的是哪一次建置；只看版本號也不夠，同一個版本號可能對應不同 commit。舊系統沒有此功能，屬情境 ② 全新功能，使用者已決定要做（延後事項 DEF-016）。

## Clarifications（釐清紀錄）

### Session 2026-10-08

背景：使用者授權由 AI 直接決定以下細節。

- Q: 起始版本號？ → A: **`0.1.0`**。系統尚未上線、API 仍在變動（如 SPEC-064 剛把識別碼改為 UUIDv7），依 SemVer 以 0.x 表示開發階段；正式上線、API 穩定時升 `1.0.0`。SDK 預設的 `1.0.0` 從未發佈，不算降版。
- Q: 版本號放哪裡、要不要用 MinVer 等工具從 git tag 推算？ → A: 手動寫在根目錄 `Directory.Build.props` 的 `<Version>`，所有專案共用。專案目前沒有發版流程與 tag，引入 MinVer 要新增套件且依賴 tag 紀律；等建立 CI 與發版流程時再評估（UDS 建議 git-height 版本推算，屬 recommended）。
- Q: sha 從哪來？ → A: .NET SDK 內建 Source Link，建置時自動把 HEAD 的完整 commit sha 附在 `AssemblyInformationalVersion` 的 `+` 之後（如 `0.1.0+f3b3a12…`）。sha 是建置時寫進組件的，能對應實際部署的檔案，不是執行時自行回報。沒有 git 資訊（如從原始碼壓縮檔建置）時為 `null`。
- Q: 建置時間怎麼寫入？ → A: Release 建置以 `AssemblyMetadata("BuildTime", …)` 寫入，格式 ISO 8601 含時區（如 `2026-10-08T19:50:00+08:00`）。Debug 建置不寫入、回 `null`：每次建置時間都不同會讓組件每次重新編譯，拖慢開發時的建置與測試；部署一律用 Release 建置，不影響發版驗證。
- Q: 要不要登入才能呼叫？ → A: **允許匿名**（`AllowAnonymous()`，憲章原則 I 要求刻意開放須附理由）。用途是部署後由維運人員或腳本確認版本，不應依賴業務帳號；回應只有版本號、sha、建置時間，不含套件版本、環境名稱、主機名稱等可協助攻擊的資訊。
- Q: 正式環境也提供嗎？ → A: 是。這個端點就是給已部署的服務用的（與只在開發環境開放的健康檢查、API 文件不同）。

## Requirements（需求）

### Requirement: US1 查詢線上建置識別（P1）

The system SHALL 提供 `GET /version`，回傳目前執行中建置的版本號、commit sha 與建置時間。

發版後維運人員或部署腳本不需登入即可確認線上執行的版本與 commit 是否與發版的一致。涵蓋 FR-001～FR-005。

#### Scenario: AC-1 未登入即可查詢建置識別
- **GIVEN** 服務已啟動
- **WHEN** 未登入呼叫 `GET /version`
- **THEN** 回 200，內容含 `version`、`sha`、`buildTime` 三個欄位

#### Scenario: AC-2 版本號與 sha 來自建置時寫入的組件資訊
- **GIVEN** 服務以 git 版本庫中的原始碼建置
- **WHEN** 呼叫 `GET /version`
- **THEN** `version` 為 SemVer 格式且等於組件設定的版本號；`sha` 為 40 碼小寫十六進位的 commit sha，等於建置時寫入組件的值

#### Scenario: AC-3 解析組件資訊
- **GIVEN** 組件版本字串與建置時間中繼資料
- **WHEN** 系統解析建置識別
- **THEN** `0.1.0+<sha>` 解析為版本 `0.1.0` 與該 sha；沒有 `+` 時 sha 為 `null`；建置時間為合法的 ISO 8601 時解析為含時區的時間，沒有或格式錯誤時為 `null`

### 功能需求條文（Functional Requirements）

- **FR-001**: 版本號 MUST 採 SemVer（`MAJOR.MINOR.PATCH`），只設定在根目錄 `Directory.Build.props` 的 `<Version>`。
- **FR-002**: 建置 MUST 把 HEAD 的完整 commit sha 寫入組件；Release 建置 MUST 另寫入建置時間（ISO 8601 含時區）。
- **FR-003**: 系統 MUST 提供 `GET /version`，回傳 `version`、`sha`、`buildTime`；取不到的值為 `null`，欄位不省略。
- **FR-004**: `GET /version` MUST 允許匿名存取，且在所有環境都提供。
- **FR-005**: 回應 MUST NOT 包含版本號、sha、建置時間以外的建置或環境資訊。

### 邊界情境（Edge Cases）

- 沒有 git 資訊的建置：`sha` 為 `null`，`version` 照常回傳。
- Debug 建置：`buildTime` 為 `null`。
- 工作目錄有未提交的修改時建置：sha 仍為 HEAD，不標示修改；發版必須從乾淨的 commit 建置。

### 假設（Assumptions）

- 部署一律用 Release 建置。
- 版本號由開發者依 `docs/INFRASTRUCTURE.md` §4 的規則在發版前手動調整；何時升版不在本規格範圍。

## Acceptance Criteria（驗收條件）

| AC | 來源 | 驗收條件 | 對應測試 |
|----|------|---------|---------|
| AC-1 | US1、FR-003、FR-004、FR-005 | 未登入呼叫 `GET /version` 回 200，內容恰為 `version`、`sha`、`buildTime` 三個欄位 | `TcmClinic.Api.AcceptanceTests/其他/版本號與建置識別/版本號與建置識別.feature` › 未登入即可查詢建置識別 |
| AC-2 | US1、FR-001、FR-002 | `version` 為 SemVer 且等於組件版本；`sha` 為 40 碼十六進位且等於組件寫入值 | `TcmClinic.Api.AcceptanceTests/其他/版本號與建置識別/版本號與建置識別.feature` › 版本號與 sha 來自組件資訊 |
| AC-3 | FR-002、FR-003 | 版本字串與建置時間的解析規則 | `TcmClinic.Api.UnitTests/Web/BuildInfoTests.cs`（單元，不寫 Gherkin：屬內部解析邏輯，不經 API） |

## Technical Design（技術設計）

### 技術脈絡

- 不新增套件：sha 用 .NET SDK 內建 Source Link 寫入的 `AssemblyInformationalVersion`；建置時間用 MSBuild 的 `AssemblyMetadata` 項目。
- 端點為 Web 層的主機資訊，不經用例層（比照 `POST /auth/logout`）。

### 受影響元件

| 層 | 元件 |
|----|------|
| 建置 | `Directory.Build.props` 加 `<Version>0.1.0</Version>`；`TcmClinic.Api.Web.csproj` 於 Release 加 `AssemblyMetadata BuildTime` |
| Web | `Endpoints/Version/BuildInfo.cs`（解析組件資訊，啟動時算一次）；`Endpoints/Version/GetVersionEndpoint.cs` |

### API 契約

`GET /version`（匿名）

```json
{
  "version": "0.1.0",
  "sha": "f3b3a123a183320c57eed715c303e017ceabd31d",
  "buildTime": "2026-10-08T19:50:00+08:00"
}
```

| 欄位 | 型別 | 說明 |
|------|------|------|
| `version` | string | SemVer 版本號 |
| `sha` | string \| null | 建置時 HEAD 的完整 commit sha；沒有 git 資訊時為 `null` |
| `buildTime` | string \| null | 建置時間（ISO 8601 含時區）；Debug 建置為 `null` |

### 設計決策（原 research）

#### R1 版本號手動設定在 Directory.Build.props
- **決定**：`<Version>` 手動維護。
- **理由**：不新增套件；目前沒有發版流程與 tag。
- **替代方案**：MinVer／Nerdbank.GitVersioning 由 tag 與 commit 高度推算（UDS recommended），待有 CI 與發版流程再評估。

#### R2 sha 用 SDK 內建 Source Link
- **決定**：讀 `AssemblyInformationalVersion` 的 `+` 之後。
- **理由**：建置時寫入、對應實際檔案，不需自訂 MSBuild 目標呼叫 git。

#### R3 建置時間只在 Release 寫入
- **決定**：`'$(Configuration)' == 'Release'` 才加 `AssemblyMetadata`。
- **理由**：避免開發時每次建置都重新編譯；部署用 Release。
- **替代方案**：執行時讀組件檔的修改時間（複製或解壓縮時可能改變，不可靠）。

#### R4 匿名存取
- **決定**：`AllowAnonymous()`。
- **理由**：發版驗證不應依賴業務帳號；回應不含敏感資訊（FR-005）。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)：原則 I（安全）——匿名開放附理由、回應只含必要資訊；原則 III（分層）——主機資訊留在 Web 層；原則 VII（繁中）——規格與註解為繁中。PASS。

## Test Plan（測試計畫）

### 自動化測試

- 單元：`BuildInfoTests`——版本字串與建置時間解析（AC-3）。
- BDD：`TcmClinic.Api.AcceptanceTests/其他/版本號與建置識別/版本號與建置識別.feature`（2026-10-09 自功能測試 `Features/version.feature` 移入）（Reqnroll，真實 WebApplicationFactory）——匿名 200、欄位、版本與 sha 對應組件（AC-1、AC-2）。

### 手動走查

- 以 Release 建置後呼叫 `GET /version`，確認 `buildTime` 有值；結果記在實作紀錄。

## Dependencies（相依）

- 無。

## Risks（風險）

- 開發者忘記在發版前升版號：同一版本號會對應多個 commit；以 sha 為準可辨識，升版紀律待建立發版流程時處理。

## 實作紀錄（Archive）

- **狀態**：已實作並合併歸檔（2026-10-08），分支 `feature/065-version-endpoint`；延後事項 DEF-016 結案。
- **實作**：`Directory.Build.props` 加 `<Version>0.1.0</Version>`；`TcmClinic.Api.Web.csproj` 於 Release 加 `AssemblyMetadata BuildTime`（`yyyy-MM-ddTHH:mm:sszzz`）；`Endpoints/BuildIdentity/BuildInfo.cs`（`Parse`、`FromAssembly`、`Current`）與 `GetVersionEndpoint`（`AllowAnonymous`）。
- **連帶修正**：`SystemParameterSteps` 原本沒有限定 feature，Reqnroll 的 `[BeforeFeature]`／`[AfterScenario]` 會套用到所有 feature；新增第二個 feature 後改以 `[Scope(Feature = "系統參數維護")]` 限定，`VersionSteps` 同樣以 `[Scope(Feature = "版本號與建置識別")]` 限定。
- **TDD 紅燈證據**（本機驗證，未提交）：先以會丟 `NotImplementedException` 的空殼讓 `BuildInfoTests` 可編譯，8 個測試全部失敗；`version.feature` 2 個場景失敗——第一次因 feature hook 以場景標籤篩選而未執行（改用 `[Scope]`），之後 AC-1 為 `Expected: OK, Actual: Unauthorized`（端點不存在，被全域 deny-by-default 擋下）、AC-2 因沒有回應內容而失敗。
- **BDD**：AC-1、AC-2 以 `版本號與建置識別.feature` 表達；AC-3 為內部解析邏輯，以單元測試表達（docs/TESTING.md §4.6）。
- **手動走查（2026-10-08）**：Release 建置的 `TcmClinic.Api.Web.dll` 含 `0.1.0+f3b3a123a183320c57eed715c303e017ceabd31d` 與 `BuildTime 2026-10-08T19:57:33+08:00`；Debug 建置只有版本與 sha，沒有建置時間，符合 R3。
- **測試數**：建置 0 錯誤 0 警告；單元 321（+8）、整合 156、功能 319（+2）全綠。
- **驗證（verify，2026-10-08）**：PASS。AC-1～AC-3 皆有程式與測試對應且通過。
- **審查（code-review，2026-10-08）**：BLOCKING 1 項已修正——端點資料夾原名 `Endpoints/Build/`，被根目錄 `.gitignore` 的 `build/`（Windows 不分大小寫）排除而沒有進 commit，本機測試全綠但乾淨 clone 會編譯失敗；改名為 `Endpoints/BuildIdentity/`，並以乾淨 clone 重新建置驗證。SUGGESTION 1 項已採納：步驟取 sha 前先斷言版本字串含 `+`。
- **合併**：PR #10，squash 合併至 master（2026-10-08）；規格狀態改為 Archived。
