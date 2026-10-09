# 基礎設施與專案結構（Infrastructure）

> 版本 1.7.0｜最後更新 2026-10-09

> 本檔為憲章原則 III（架構一致性）的結構性 **How**，涵蓋 Monorepo 佈局、Aspire 編排、
> 新專案建立與 Build/CI。多屬一次性設定。

## 1. Monorepo 結構

本工作流涵蓋後端與前端（2026-10-09 起，憲章 2.2.0）。儲存庫佈局：

```
project-root/
├── .standards/               # UDS 開發標準（規格驅動流程見 spec-driven-development）
├── .githooks/                # 共用 git hook（gitleaks 秘密掃描、uds check）
├── .project-context/         # 專案決策記憶（UDS project-context-memory）
├── .memory/                  # 開發者經驗（UDS developer-memory）
├── docs/                     # 憲章（CONSTITUTION.md）與實作規範（How）
├── specs/                    # 功能規格（UDS SDD 格式，依選單分層）
├── {Project}.slnx            # solution 涵蓋 apphost/ + backend/src/
├── Directory.Build.props      # 共用 MSBuild props（repo 根）
├── Directory.Packages.props   # Central Package Management（repo 根）
├── apphost/                   # 全系統組合根（與各堆疊平行）
│   └── {Project}.AspireHost/  # 編排系統資源
├── backend/
│   ├── src/
│   │   ├── {Project}.Core/
│   │   ├── {Project}.UseCases/
│   │   ├── {Project}.Infrastructure/
│   │   ├── {Project}.Web/
│   │   └── {Project}.ServiceDefaults/   # 後端共用；由 Web 參照
│   ├── tests/
│   │   ├── {Project}.UnitTests/
│   │   ├── {Project}.IntegrationTests/
│   │   ├── {Project}.FunctionalTests/
│   │   ├── {Project}.AcceptanceTests/   # 驗收（BDD），一個選單一個資料夾，路徑同 specs/
│   │   └── Shared/                      # 多個測試專案共用的檔案，以連結納入
│   └── .editorconfig
├── frontend/                  # pnpm workspace（結構見 docs/FRONTEND.md §2）
│   ├── shared/                # 共用 API 型別（由 OpenAPI 產生）與純函式
│   ├── site/                  # 前台：Nuxt 4（SSR）
│   └── admin/                 # 後台：Vue 3 + Vite（SPA）
├── prototype/                 # 前台設計雛形（靜態 HTML，參考用，不建置）
├── THIRD-PARTY-NOTICES.md
└── README.md
```

規則：

- 後端業務/應用程式碼專屬於 `backend/`（Core、UseCases、Infrastructure、Web、ServiceDefaults、tests）。
- **AspireHost** 是全系統組合根，位於頂層 `apphost/`（與 `backend/` 平行），是「後端程式碼專屬於 backend/」的唯一例外——它是編排而非業務碼，以相對路徑參照後端專案（`../backend/src/...`）。
- `.slnx` 與 CPM（`Directory.Packages.props`/`Directory.Build.props`）置於 repo 根，讓 solution 涵蓋 `apphost/` 與 `backend/src/`。
- `{Project}.ServiceDefaults` 留在 `backend/src/`（後端共用，由 Web 參照），不移到 `apphost/`。
- 共用 API 契約以後端產生的 OpenAPI 為唯一來源；前端由它產生型別到 `frontend/shared/`（`docs/FRONTEND.md` §5.2），不另外維護契約檔。
- 前端程式碼專屬於 `frontend/`，不放在 `backend/` 或 repo 根；.NET solution 不包含前端。
- CI 管線必須用 path filter 避免重建未受影響的部分（`backend/`、`apphost/` 變更跑後端，`frontend/` 變更跑前端）。

## 2. Aspire 編排

**AspireHost** 專案定義開發拓撲：

- SQL Server 容器用 `ContainerLifetime.Persistent`（開發期跨重啟存活）。
- 連線字串經 `.WithReference()` 注入。
- 服務用 `.WaitFor()` 宣告啟動相依。
- **ServiceDefaults**（在 `backend/src/`）提供共用設定：OpenTelemetry、health check 端點（`/health`、`/alive`）、service discovery、HTTP resilience。

### HTTPS 重導、HSTS 與安全標頭

正式環境必須具備 HTTPS 重導、HSTS 與安全標頭（`docs/SECURE-CODING.md` §6）。由程式或 IIS 哪一層負責尚未決定，目前程式也未設定（`Program.cs` 沒有 `UseHttpsRedirection`／`UseHsts`），見 specs/README.md 延後事項 DEF-006。決定後在此記錄。

若由程式負責：用 Aspire DCP proxy 時，開發環境必須停用（或條件跳過）`UseHttpsRedirection()`。DCP proxy 暴露 HTTP 端點，HTTPS 重導會造成瀏覽器用戶端 CORS preflight 失敗（OPTIONS 收到 302 破壞瀏覽器 CORS）。

```csharp
// Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

此為本機 DCP proxy 例外，**非生產許可**（見 `docs/SECURE-CODING.md` §6）。

### Infrastructure 連線字串解析順序

1. `"DefaultConnection"` — 由 Aspire（`sqlServer.AddDatabase("DefaultConnection", …)`）、設定檔或環境變數提供；正式環境必須提供
2. 未提供時退回本機 localhost SQL Server（`Trusted_Connection=True;Encrypt=False`，僅供開發）

只使用 SQL Server，不使用 SQLite（`InfrastructureServiceExtensions.cs`）。

## 3. 建立新專案（Greenfield）

- 新專案一律由後端範本建立：複製範本後依範本根目錄 `NEW-PROJECT.md` 的步驟執行 `init-project.ps1 -Name <專案名>`，把範本代號全面改為實際專案名稱（兩檔在初始化完成後刪除）。範本已含可編譯、測試全綠的骨架（使用者與角色、登入、選單授權、稽核與日誌、四層測試），不再另跑 scaffold 產生程式碼。
- 建立後先確認 build 與四層測試全綠，再開始第一個規格（範本內建的帳號維護空殼 `specs/管理業務/系統管理作業/帳號維護/spec.md` 可作為第一份規格的起點）。
- 範本附的 `THIRD-PARTY-NOTICES.md` 與骨架相依一致；之後依 `docs/DEPENDENCIES.md` 隨相依變更維護。

## 4. Build & CI

```bash
dotnet build TcmClinic.Api.slnx
# 測試以 dotnet run --project 執行（xUnit v3；dotnet test 在本專案會失敗）
dotnet run --project backend/tests/TcmClinic.Api.UnitTests
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests
dotnet run --project backend/tests/TcmClinic.Api.AcceptanceTests

# 前端（在 frontend/ 執行，見 docs/FRONTEND.md §10）
pnpm install --frozen-lockfile
pnpm lint
pnpm typecheck
pnpm test
```

- **共用 git hook**：首次 clone 後執行 `git config core.hooksPath .githooks` 啟用 `.githooks/pre-commit`（commit 前以 gitleaks 掃描秘密，再執行 `uds check`）。gitleaks 以 `winget install Gitleaks.Gitleaks` 安裝；未安裝時 commit 會被擋下。改用 `core.hooksPath` 後 `.git/hooks/` 下的 hook 不再執行，原本 `uds init` 產生的 UDS 檢查已併入共用 hook。
- 合併前必須通過 build ＋單元、整合、功能、驗收四層測試；有改到 `frontend/` 時，前端的 `lint`、`typecheck`、`test` 也必須全綠（目前由作者在本機執行。`.github/workflows/ci.yml` 已備妥，但公司 GitHub Enterprise 停用 hosted runner，執行必定失敗，2026-10-08 起於 GitHub 停用此 workflow；有可用 runner 後以 `gh workflow enable CI` 恢復，改由 CI 執行）。
- CI 建立時必須驗證跨平台建置（Windows、Linux、macOS）。
- **部署前確認網路架構**（SPEC-004 FR-006b）：登入限速與請求日誌的 `source_ip` 都取 `RemoteIpAddress`。IIS 同程序部署且前面沒有代理時即為用戶端 IP；若 IIS 前另有負載平衡或反向代理，必須設定 `UseForwardedHeaders` 並以 `KnownProxies`／`KnownNetworks` 限定可信任的代理，否則所有用戶端共用一份登入額度，且 `X-Forwarded-For` 可被偽造。
- **版本號採 SemVer**（`MAJOR.MINOR.PATCH`，2026-10-08 裁決，對齊 UDS）：API 有破壞性變更（移除欄位、改型別、改錯誤語意）升 MAJOR；新增功能升 MINOR；修正升 PATCH。版本號集中設定一處（如 `Directory.Build.props`），建置時一併寫入 git commit sha 與建置時間。
- **建置識別端點**：部署的服務提供 `GET /version`，回傳 `version`、`sha`、`buildTime`；發版後以此端點確認線上的 sha 與發版 commit 相符。允許匿名存取，所有環境都提供（SPEC-065 R4）。
- 版本號設在根目錄 `Directory.Build.props` 的 `<Version>`（目前 `0.1.0`，正式上線、API 穩定時升 `1.0.0`）；sha 由 SDK 的 Source Link 附在 `InformationalVersion` 的 `+` 之後；建置時間只在 Release 建置寫入（Debug 為 `null`），部署一律用 Release 建置，且須從乾淨的 commit 建置（SPEC-065）。

## 5. Git 流程

> 採 UDS 流程（GitHub Flow＋squash，憲章 2.1.0）。

- **分支**：每個規格一條分支，命名 `feature/NNN-短名`（小寫英文，如 `feature/058-account-maintenance`），與 SPEC-ID 對應；修 bug 用 `fix/NNN-短名`，非規格工作用 `feature/<短名>`（如 `feature/upgrade-ef-core`）。
- **提交**：依 `.github/COMMIT_CONVENTION.md`（繁中類型）。**每個 commit 都必須建置成功、測試全綠**（UDS checkin）；TDD 紅燈只在本機驗證，執行結果記在規格的「實作紀錄」（憲章原則 II，見 `docs/TESTING.md` §1）。AI 在階段完成且全綠後自動 commit（2026-10-06 起）。
- **合併**：AI 自動 push 分支並開 PR，合併前跑 `code-review` 與 `checkin`、build 與四層測試全數通過，以 **squash** 合併回 master（UDS 選項 `squash-merge`），合併訊息附 PR 編號，合併後刪除分支。沒有 BLOCKING 時由 AI 自動完成（2026-10-08 起）。
- **審查**：PR 以 Code Review 逐條對照憲章原則。
- **push**：AI 在合併流程中自動 push 功能分支；不得 force push，不得直接 push 到 master（一律經 PR 合併）。
- **CHANGELOG**：不維護；功能進度以 `specs/README.md` 為準。
- **worktree**：若使用 git worktree，目錄放在 `.claude/worktrees/`（已在 `.gitignore`）。
