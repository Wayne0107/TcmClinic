# 測試規範（Testing）

> 版本 1.2.0｜最後更新 2026-10-09

> 本檔為憲章原則 II（測試優先，程式碼必須可測試）的 **How**。

## 1. TDD 循環（強制）

新行為必須測試先行，遵循 Red-Green-Refactor：

1. **Red**：寫一個失敗的測試，確認它因「行為缺失」而失敗（非無關的編譯錯誤）。
2. **Green**：寫剛好讓測試通過的最小實作（YAGNI）。
3. **Refactor**：在測試保持綠燈下改進設計。

- **紅燈證據**（2026-10-06 起，憲章 2.1.0）：紅燈只在本機執行，確認失敗原因是「行為缺失」；把紅燈測試的執行摘要（測試名稱、失敗原因、日期）記在規格的「實作紀錄」。每個 commit 都必須全綠，紅燈不提交。
- **Bug 修復**：每個缺陷修復必須先寫一個重現該缺陷的失敗回歸測試，該測試轉綠且既有測試全綠才算完成。
- **緊急修復不例外**：時間緊迫仍須寫測試。

### 各層嚴格度

- **單元（MUST，嚴格）**：領域模型、值物件、use case handler、純邏輯——嚴格逐輪 Red-Green-Refactor。
- **整合（MUST）**：Repository/EF Core 行為、規格、基礎設施——實作前先有失敗測試。
- **功能（SHOULD，契約優先）**：API 端點行為先以可執行契約定義；因為需要 Testcontainers + Docker 較慢，允許在端點層批次紅綠，但「無任何前置失敗/契約測試就實作端點」是禁止的。

### 明確排除（不需測試先行）

純接線與設定且無分支邏輯：DI 註冊、`Configure()` metadata、migrations、`Program.cs`/`*Config.cs` 接線、專案 scaffold。一旦加入邏輯即回到測試先行規則。

## 2. 測試層級

| 層級 | 測試對象 | 工具 |
|------|---------|------|
| **單元** | 核心業務邏輯、領域模型、use case handler | xUnit + NSubstitute |
| **整合** | 資料庫操作、基礎設施、外部服務互動 | xUnit + Testcontainers（SQL Server 2022） |
| **功能** | API 端點（`WebApplicationFactory` 皮下測試） | `Microsoft.AspNetCore.Mvc.Testing` |
| **驗收（BDD）** | 規格驗收條件的 Gherkin 情境（`/derive-bdd` 產出），步驟定義呼叫 API 端點；獨立專案 `TcmClinic.Api.AcceptanceTests`，一個選單一個資料夾（§4.6） | Reqnroll＋xUnit（首次引進時依 `docs/DEPENDENCIES.md` 審查授權） |

- 測試專案必須對映其所測試的層：`TcmClinic.Api.UnitTests`、`TcmClinic.Api.IntegrationTests`、`TcmClinic.Api.FunctionalTests`、`TcmClinic.Api.AcceptanceTests`。
- `TreatWarningsAsErrors` 與 nullable reference types 全域啟用。
- 各層不設數量比例目標（UDS 6.13 也已棄用 70/20/7/3 金字塔，改用「覆蓋率只升不降」）；驗收以 BDD 情境＋功能測試表達（§4.6）。本專案只涵蓋後端，不設 E2E（瀏覽器）層。
- 驗收（BDD）層自 2026-10-06 起只套用新規格；已完成的規格不回頭補 `.feature`。

### Mock 邊界

- **單元測試**可在 port 邊界以 NSubstitute 替換 repository 與外部服務介面（如 `IRepository<T>`、`IUserLogRecorder`），驗證 handler 邏輯；真實資料庫行為由整合與功能測試涵蓋。新寫的 mock 前一行加 `// MOCK: <理由>`。
- **整合與功能測試不得 mock 自家服務**（UseCases、Infrastructure 的實作），一律用真實實作與 Testcontainers 資料庫；整合測試專案不參考 NSubstitute。
- 功能測試可用 `TestAuthHandler` 替換「驗證」以指定登入身分，但**授權必須是真實的**（選單授權 `MenuAccess` 照常運作）；登入流程本身另以 `AuthWebApplicationFactory` 用真實 Cookie 測試。

## 3. 命名慣例

- 測試類別＝`{被測對象}Tests`，例：`CreateUserHandlerTests`、`UserCrudEndpointsTests`。
- 測試方法＝以底線分隔、描述預期行為的英文句子，例：`Save_failure_with_existing_code_returns_duplicate_error`、`List_pages_by_page_size`。
- 測試意圖另以繁中 `DisplayName` 表達（§5）。

## 4. AI 防護測試機制（Testing Constitution）

以下是超越「測試層級」的具體機制，用於攔截 AI 生成程式碼最常見的失效模式。

### 4.1 屬性測試（MUST）— CsCheck

核心領域邏輯必須以屬性測試對隨機/邊界輸入驗證不變式，不能只靠手挑範例。必測範圍：

- 金額/數量計算（折扣、紅利、稅）。
- 狀態機轉換（SmartEnum）。
- 值物件 `Validate` 規則（有效輸入接受、無效輸入拒絕）。
- 關鍵邊界的 guard clauses。

範圍必須聚焦於「有真實不變式的邏輯」，非 CRUD 接線。

```csharp
// 情境：所有落在 2..100 長度的名稱都應被 ContributorName 接受（不變式）。
[Fact(DisplayName = "有效長度名稱皆被 ContributorName 接受")]
public void ContributorName_Accepts_All_Valid_Names()
{
    Gen.String[Gen.Char.Letter, 2, 100].Sample(name =>
        ContributorName.From(name).Value.Length is >= 2 and <= 100);
}
```

### 4.2 契約測試（MUST）— Verify

公開 API 契約以 snapshot/approval 測試鎖定：

- 產生的 OpenAPI 文件（`/openapi/*.json`）。
- 端點 Response DTO 形狀與狀態碼集合。

任何欄位名、型別、可空性、狀態碼變動都必須讓 snapshot 測試失敗，並在同一 PR 中刻意重新核准，讓契約變更明確可審。AI 不得無聲地重新產生 snapshot 略過人為審查。

### 4.3 架構測試（MUST）— NetArchTest

Clean Architecture 依賴規則必須以可執行架構測試強制：

- Core 不得依賴 UseCases / Infrastructure / Web。
- UseCases 不得依賴 Infrastructure / Web。
- 違反則建置失敗。

### 4.4 變異測試（SHOULD）— Stryker.NET

Core、UseCases 專案宜定期（如每週）或發版前執行變異測試；**不得**放進 commit hook，也不在每個 PR 執行（2026-10-08 裁決，對齊 UDS：變異測試耗時，每次都跑會拖慢回饋）。**變異分數**是測試有效性的信號；**不得**以行覆蓋率百分比作為品質門檻（無法偵測空斷言）。覆蓋率只要求「不得下降」（憲章），不設百分比門檻。

`backend/stryker-config.json` 預設變異 Core；UseCases 以 `--project` 另外執行（於 `backend/` 目錄）：

```bash
dotnet stryker                                   # TcmClinic.Api.Core
dotnet stryker --project TcmClinic.Api.UseCases.csproj
```

- **現況（2026-10-08 實測）**：Stryker.NET 5.0.0 在本專案（xUnit v3／MTP）無法產生有效分數——預設 vstest 執行器全判逾時，`--test-runner mtp` 會誤判 Killed。工具修正前不以變異分數作為依據，見 specs/README.md 延後事項 DEF-021。
- `thresholds.break` 維持 0：分數僅作信號，不擋合併（不採 UDS「AI 產生的測試低於 50% 即退回」等硬門檻）。

### 4.4.1 覆蓋率基準（不得下降）

測試以 `dotnet run` 執行，`coverlet.collector`（僅支援 `dotnet test`）無法量測，改用 `.config/dotnet-tools.json` 的 `coverlet.console`。三層依序執行並以 `--merge-with` 合併成一份總覆蓋率（於 repo 根目錄，先 `dotnet build TcmClinic.Api.slnx`）：

```bash
OPT=(--include "[TcmClinic.Api.*]*" --exclude "[TcmClinic.Api.*Tests]*" --exclude-by-file "**/Migrations/*.cs" \
     --exclude-by-attribute "GeneratedCode,CompilerGenerated,ExcludeFromCodeCoverage")
cov() { B=backend/tests/$1/bin/Debug/net10.0; dotnet coverlet $B/$1.dll --target dotnet --targetargs "$B/$1.dll" "${OPT[@]}" "${@:2}"; }
cov TcmClinic.Api.UnitTests        --format json --output coverage/unit/
cov TcmClinic.Api.IntegrationTests --format json --output coverage/integ/ --merge-with coverage/unit/coverage.json
cov TcmClinic.Api.FunctionalTests  --format json --format cobertura --output coverage/all/ --merge-with coverage/integ/coverage.json
```

基準（三層合併，排除 migration 與產生的程式碼；範本未附數字，新專案建立後首次量測時填入，日期與量測時點寫在本行）：

| 模組 | 行 | 分支 | 方法 |
|------|----|------|------|
| Core | – | – | – |
| Infrastructure | – | – | – |
| Web | – | – | – |
| UseCases | – | – | – |
| SharedKernel | – | – | – |
| ServiceDefaults | – | – | – |
| **總計** | **–** | **–** | **–** |

- 合併前若總計的行或分支覆蓋率低於上次基準，須說明原因；新增功能使覆蓋率上升時更新本表。
- 數字只用來判斷「有沒有下降」，不作品質門檻（見 §4.4）。
- 維持人工比對；基準檔加 PR 自動比對、未達標即失敗（UDS）暫不做，CI 可用後再議（2026-10-08 使用者決定，見 specs/README.md 延後事項 DEF-018）。

### 4.5 邊界與錯誤處理最低要求（MUST）

有分支的邏輯，單元測試必須涵蓋錯誤/例外路徑與至少三個邊界（如 null、空集合、極值、併發）。測試必須斷言可觀察行為；空斷言或恆真測試（`Assert.True(true)`）禁止並須於審查中退回。

### 4.6 驗收＝BDD 情境＋功能測試

- 新規格（2026-10-06 起）核准後先跑 `/universal-dev-standards:derive-bdd`，把驗收條件轉成 Gherkin `.feature`；每個 Scenario 標 `@SPEC-NNN @AC-N`。
- 驗收情境放在獨立專案 `backend/tests/TcmClinic.Api.AcceptanceTests`（2026-10-09 起），**一個選單一個資料夾，路徑與 `specs/` 相同**：規格在 `specs/其他/版本號與建置識別/spec.md`，情境就在 `TcmClinic.Api.AcceptanceTests/其他/版本號與建置識別/版本號與建置識別.feature`。`.feature` 以選單名命名；步驟定義放同一資料夾，類別與檔名用英文（如 `VersionSteps.cs`），命名空間一律 `TcmClinic.Api.AcceptanceTests`（選單名可能含「、」等不能當識別字的字元）。
- 步驟定義（Reqnroll）呼叫 API 端點，跑真實資料庫（`WebApplicationFactory` + Testcontainers）。步驟類別以 `[Scope(Feature = "<功能名稱>")]` 限定所屬 feature，hook 才不會套用到其他選單。
- 功能與驗收測試共用的工廠與輔助（`CustomWebApplicationFactory`、`AuthWebApplicationFactory`、`TestAuthHandler`、各選單的 `*TestSupport`）放 `backend/tests/Shared/`，命名空間 `TcmClinic.Api.Tests.Shared`，以 `<Compile Include="../Shared/X.cs" Link="Shared/X.cs" />` 連結納入需要的專案；只有一個專案用到的輔助留在該專案。
- 情境寫不出來或不值得寫成 Gherkin 的驗收條件（如查詢次數、契約快照），照舊用功能或整合測試表達，並在規格驗收表註明。

## 5. 測試意圖註解與 DisplayName（MUST）

- 每個測試必須帶一段簡短註解，說明「情境/前提」與「預期行為」；屬性測試則說明其不變式；有需求 id 時引用（如 FR-004）。
- 每個 `[Fact]`/`[Theory]` 必須宣告繁體中文 `DisplayName`，讓測試意圖出現在測試執行輸出中。
- 驗證某條規格驗收條件的測試，必須在屬性上方以 `// @SPEC-NNN @AC-N` 標記（一行一組，可多行）；規格 AC 表「驗證項」欄列出的測試須與標記一致，供 `/sdd analyze` 檢查 AC↔測試追溯（UDS acceptance-criteria-traceability）。

```csharp
// 情境：刪除仍有子項的父選單 → 應阻擋（FR-004），不可連動刪除整棵子樹。
// @SPEC-002 @AC-19
[Fact(DisplayName = "刪除仍有子項的父選單時應阻擋，不連動刪除子樹（FR-004）")]
public async Task Delete_blocks_when_item_has_children() { /* ... */ }
```

## 6. 測試基礎設施

- **Testcontainers**：整合、功能與驗收測試共用 Docker 內的一個 SQL Server 2022 容器（`backend/tests/Shared/TestDatabase.cs`，以連結方式納入三個測試專案；同一測試程序內共用，各專案依序執行）。每個 `TestDatabase` 實例在容器中建立專屬資料庫、結束時刪除，故每個測試（整合）、每個類別（功能）或每個 feature（驗收）仍取得全新的空資料庫。新測試一律用 `TestDatabase`，不要自行 `new MsSqlBuilder()`。Docker Desktop 須執行中。
- **平行執行**：各專案 `xunit.runner.json` 開啟 `parallelizeAssembly` 與 `parallelizeTestCollections`。
- **執行方式**：用 `dotnet run --project backend/tests/<測試專案>`（xUnit v3；`dotnet test` 在本專案會失敗）；只跑某類別加 `-- -class "<完整類別名>"`。
- **WebApplicationFactory**：`CustomWebApplicationFactory`（`backend/tests/Shared/`，功能與驗收測試共用）實作 `IAsyncLifetime`——於 `InitializeAsync` 建立專屬資料庫（啟動時 Program 套用 migration），於 `DisposeAsync` 刪除。

## 7. Code Review 測試閘門

- 缺少 TDD 證跡（規格「實作紀錄」沒有紅燈測試的執行摘要）→ 退回。
- 測試缺少測試意圖註解或 `DisplayName` → 退回。
- 空斷言 / 恆真測試 → 退回。
- 僅為拉高覆蓋率而加的測試 → 退回。
- 每個 commit 與合併前所有測試都必須通過（100%），不接受通過率門檻。
- 不採用的 UDS 測試做法：E2E（瀏覽器）層、ATDD 工作坊與 PO 簽核、TESTPLAN 使用者旅程、跨測試共享狀態的流程測試、每個 public 方法三條路徑、`@ac` 標記（統一用 `// @SPEC-NNN @AC-N`，`.feature` 用 `@SPEC-NNN @AC-N` 標籤）。
