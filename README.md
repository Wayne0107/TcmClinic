# TcmClinic

<!-- 建立新專案後，把本段改成實際專案的簡介（系統用途、使用者、取代或整合的對象）。 -->
（專案簡介待填）

本 repo 以 **.NET Clean Architecture** 實作後端 API，以 **.NET Aspire** 編排本機開發環境，並以 **UDS（Universal Dev Standards）規格驅動開發（SDD）** 管理需求與實作。

- **文件語言**：繁體中文（憲章第 VII 條；程式碼識別項維持英文）
- **治理範圍**：後端。前端（若有）由另一團隊維護，不在本流程的規格範圍

---

## 目錄

- [技術棧](#技術棧)
- [骨架內建功能](#骨架內建功能)
- [前置需求](#前置需求)
- [啟動與測試](#啟動與測試)
- [開發流程（UDS 規格驅動）](#開發流程uds-規格驅動)
- [專案結構](#專案結構)
- [文件索引](#文件索引)

---

## 技術棧

| 類別 | 使用 |
|------|------|
| 平台 | .NET 10、ASP.NET Core、C# |
| 架構 | Clean Architecture（Core／UseCases／Infrastructure／Web）、DDD、Vogen 強型別 ID、Ardalis.Specification／Result、Mediator |
| API | FastEndpoints（REPR）、ProblemDetails（RFC 9457）、OpenAPI＋Scalar（僅開發） |
| 資料 | SQL Server、EF Core（code-first migration）、RowVersion 樂觀併發、UUIDv7 對外識別碼 |
| 驗證 | JWT Bearer＋可撤銷 refresh token（HttpOnly Cookie）、PBKDF2 密碼雜湊、登入限速 |
| 日誌與觀測 | Serilog（生產 JSON、依等級保留）、OpenTelemetry、健康檢查 |
| 編排 | .NET Aspire（SQL Server 容器＋API） |
| 測試 | xUnit v3、NSubstitute、CsCheck、Testcontainers、Verify、Reqnroll（BDD）、NetArchTest |

## 骨架內建功能

使用者與角色、選單導向授權（選單可見性＝操作權限）、選單管理 `/admin/menus`、我的選單 `/me/menus`、角色權限維護、個人資料維護、五個標準欄位與稽核（`SysAuditLog`）、資料訪問日誌（`SysAccessLog`）、使用者操作紀錄（`SysUserLog`）、分頁（預設 10、上限 100）、`GET /version`、健康檢查。各功能的規格見 [specs/README.md](specs/README.md)。

Development 環境的種子：`SUPERADMIN` 角色、`admin` 帳號，以及「管理業務 › 系統管理作業」選單（角色權限維護、帳號維護、個人資料維護）。

---

## 前置需求

| 項目 | 說明 |
|------|------|
| **.NET SDK 10** | `Directory.Build.props` 指定 `net10.0` |
| **Docker Desktop** | Aspire 的 SQL Server 容器、整合／功能／驗收測試（Testcontainers）需要 |
| **AI 工具＋UDS** | 開發流程指令（`/universal-dev-standards:sdd`、`tdd`…）來自 UDS；標準本文已安裝在 `.standards/` |
| **gitleaks** | 每次 commit 前掃描秘密；`winget install Gitleaks.Gitleaks` |

首次 clone 後啟用專案共用的 git hook（未安裝 gitleaks 時會擋下 commit）：

```bash
git config core.hooksPath .githooks
```

---

## 啟動與測試

```powershell
# 建置
dotnet build TcmClinic.Api.slnx

# 啟動全系統（SQL Server 容器 + 後端 API；需 Docker）
dotnet run --project apphost/TcmClinic.Api.AspireHost
```

- Development 環境啟動時會自動套用 migration 並種入上述種子資料。
- API 文件只在 Development 開放：Scalar UI 位於 `/scalar`。

### 執行測試

測試用 `dotnet run --project` 執行（xUnit v3；`dotnet test` 會失敗）。整合、功能與驗收測試需要 Docker，請依序執行、不可平行：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.UnitTests
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests
dotnet run --project backend/tests/TcmClinic.Api.AcceptanceTests   # 驗收情境（Gherkin），一個選單一個資料夾

# 只跑某個測試類別
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests -- -class "TcmClinic.Api.FunctionalTests.ListPageSizeTests"
```

> 後端正在執行時再跑測試會撞到輸出檔鎖定，請加 `--artifacts-path <暫存目錄>`。

---

## 開發流程（UDS 規格驅動）

規格先於程式碼：沒有核准的規格，不寫功能程式碼（例外：緊急修復、錯字或格式）。流程依據 [`.standards/spec-driven-development.ai.yaml`](.standards/spec-driven-development.ai.yaml)：

```
Discuss → Create（寫規格）→ Review → Approve → derive-bdd → Implement → Verify → PR（squash）→ Archive
```

- 各情境（新功能、修 bug、修改既有規格、重構）要下的指令與停止點見 [docs/WORKFLOW.html](docs/WORKFLOW.html)。
- 規格依選單分層放在 `specs/<業務>/<作業>/<功能>/spec.md`，跨選單的放 `specs/其他/`；總覽、狀態、延後事項與 ADR 索引見 [specs/README.md](specs/README.md)。
- 規範優先順序：憲章 > `docs/` > 規格 > `.standards/`；已裁決的衝突與 AI 行為約定見 [AGENTS.md](AGENTS.md)。
- 提交訊息用繁體中文類型（`新增`、`修正`、`文件`…），實作類加 `Refs: SPEC-NNN`，見 [.github/COMMIT_CONVENTION.md](.github/COMMIT_CONVENTION.md)。

---

## 專案結構

```
TcmClinic/
├── .standards/                    # UDS 開發標準本文（AGENTS.md 為其索引）
├── .githooks/                     # 共用 git hook（gitleaks、uds check）
├── .github/
│   ├── COMMIT_CONVENTION.md       # 提交訊息規範（Conventional Commits，繁中）
│   └── workflows/ci.yml           # CI（目前停用，合併前本機驗證）
├── .project-context/              # 專案決策記憶
├── .memory/                       # 開發者經驗（踩雷與解法）
├── apphost/TcmClinic.Api.AspireHost/   # Aspire 編排（SQL Server、API）
├── backend/
│   ├── src/                       # SharedKernel / Core / UseCases / Infrastructure / Web / ServiceDefaults
│   └── tests/                     # UnitTests / IntegrationTests / FunctionalTests / AcceptanceTests / Shared
├── docs/                          # 憲章與實作規範（How）、開發流程
├── specs/                         # 功能規格（UDS SDD）與 ADR
├── TcmClinic.Api.slnx
├── Directory.Build.props          # 共用 MSBuild 設定、版本號
├── Directory.Packages.props       # 套件版本集中管理（CPM）
└── THIRD-PARTY-NOTICES.md         # 第三方授權聲明
```

---

## 文件索引

| 文件 | 內容 |
|------|------|
| [AGENTS.md](AGENTS.md) | 規範優先順序、已裁決的衝突、AI 行為約定、Build & Test |
| [docs/CONSTITUTION.md](docs/CONSTITUTION.md) | 專案憲章：衝突時的裁決原則（What） |
| [docs/WORKFLOW.html](docs/WORKFLOW.html) | 各開發情境的指令、停止點與產出 |
| [docs/CODING-STANDARDS.md](docs/CODING-STANDARDS.md) | 分層、DDD、Vogen 等編碼規範 |
| [docs/API-CONVENTIONS.md](docs/API-CONVENTIONS.md) | API 回應格式、狀態碼、分頁、路由 |
| [docs/DATABASE-SCHEMA.md](docs/DATABASE-SCHEMA.md) | 資料表命名、標準欄位、外鍵 |
| [docs/TESTING.md](docs/TESTING.md) | 測試分層與寫法 |
| [docs/SECURE-CODING.md](docs/SECURE-CODING.md) | 安全編碼清單 |
| [docs/INFRASTRUCTURE.md](docs/INFRASTRUCTURE.md) | Monorepo、Aspire、新專案建立、CI、Git 流程 |
| [docs/DEPENDENCIES.md](docs/DEPENDENCIES.md) | 套件管理與授權治理 |
| [docs/LOGGING.md](docs/LOGGING.md)、[docs/PERFORMANCE.md](docs/PERFORMANCE.md)、[docs/COMMENTS.md](docs/COMMENTS.md) | 日誌、效能、註解規範 |
| [specs/README.md](specs/README.md) | 規格索引、延後事項、ADR 索引 |
| [.github/COMMIT_CONVENTION.md](.github/COMMIT_CONVENTION.md) | 提交訊息規範 |
| [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) | 第三方授權聲明 |
