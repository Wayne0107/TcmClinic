# AGENTS.md
## 專案規範與優先順序（MUST，先讀）

本專案同時有**專案自己的規範**與 UDS 標準（`.standards/`）。動手前必須先讀：
1. [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md)——裁決原則（What）
2. 與任務相關的 `docs/*.md`——實作規範（How），對照表如下
3. 相關規格 `specs/**/spec.md` 與 [`specs/README.md`](specs/README.md)
4. `.standards/` 下與任務相關的 `.ai.yaml` 檔

**衝突時的優先順序：憲章 > docs/ > 規格 > `.standards/`。** `.standards/` 只在專案規範沒有規定時適用；下方「已裁決的衝突」表列出的項目一律照專案做。

| 任務 | 必讀 |
|------|------|
| 分層、DDD、Vogen、資料夾、編碼風格 | [docs/CODING-STANDARDS.md](docs/CODING-STANDARDS.md)、`.editorconfig` |
| 前端（前台 Nuxt 4、後台 Vue 3、共用型別；結構、分層、API 串接、登入、安全、測試、效能） | [docs/FRONTEND.md](docs/FRONTEND.md)、`frontend/.editorconfig` |
| 程式碼註解、語言 | [docs/COMMENTS.md](docs/COMMENTS.md) |
| API、狀態碼、分頁、路由 | [docs/API-CONVENTIONS.md](docs/API-CONVENTIONS.md) |
| 資料表、欄位、migration | [docs/DATABASE-SCHEMA.md](docs/DATABASE-SCHEMA.md) |
| 測試（TDD、繁中 `DisplayName`＋測試意圖註解、AC 測試加 `// @SPEC-NNN @AC-N`） | [docs/TESTING.md](docs/TESTING.md) |
| 安全 | [docs/SECURE-CODING.md](docs/SECURE-CODING.md) |
| 日誌 | [docs/LOGGING.md](docs/LOGGING.md) |
| 效能 | [docs/PERFORMANCE.md](docs/PERFORMANCE.md) |
| 專案結構、Aspire、建置、Git 流程 | [docs/INFRASTRUCTURE.md](docs/INFRASTRUCTURE.md) |
| 新增或升級套件 | [docs/DEPENDENCIES.md](docs/DEPENDENCIES.md)（同次變更更新 `THIRD-PARTY-NOTICES.md`） |
| 提交訊息（`<繁中類型>(<SPEC 編號或模組>): <繁中主旨>`，如 `新增(058): …`，72 字元內，實作類加 `Refs: SPEC-NNN`） | [.github/COMMIT_CONVENTION.md](.github/COMMIT_CONVENTION.md) |
| 撰寫或修改規格、ADR | [specs/README.md](specs/README.md)「如何維護」與範例 `specs/其他/開發模式可見全部選單/spec.md` |
| 開發流程（各情境要下的指令） | [docs/WORKFLOW.html](docs/WORKFLOW.html) |

### 已裁決的衝突（照專案做，不採 UDS 預設）

| 議題 | 本專案做法 | 不採用的 UDS 預設 |
|------|-----------|------------------|
| 資料表與欄位命名 | PascalCase 單數；系統表 `Sys` 前綴；外鍵 `XxxId`；索引沿用 EF 預設 `IX_`／`FK_`／`PK_` | snake_case、`idx_`／`uq_` |
| 標準欄位 | 所有表五欄 `CreateDate`／`CreateUser`／`ModifyDate`／`ModifyUser`／`Removed`；`datetime` 存當地時間 | `created_at`／`updated_at`、TIMESTAMPTZ、UTC |
| 驗證與業務規則失敗 | 400 ValidationProblem；只有狀態衝突回 409 | 422 |
| 分頁 | `page`／`pageSize`，回應 `items`／`page`／`pageSize`／`totalCount` ＋ Link header；預設 10 筆、上限 100（ADR-007）；原則上一律 offset，有效能需求時再於該規格評估 keyset（2026-10-08 使用者決定） | `limit`、`{data,meta,links}` 信封、預設 20 |
| 路由 | 資源複數 kebab-case；狀態轉換允許 `POST /{resource}/{id}/{action}`；不做 URL 版本 | 禁止動詞、`/v1/` |
| 錯誤代碼、速率限制標頭、MFA | 不採用（規格未要求的不新增）；登入端點依來源 IP 限速，每分鐘 20 次、超過回 429（SPEC-004 FR-006b） | 錯誤代碼體系、X-RateLimit-*、MFA |
| 測試分層 | 單元、整合、功能三層＋驗收（BDD：新規格以 Gherkin `.feature` 描述驗收情境，獨立專案 `TcmClinic.Api.AcceptanceTests`，一個選單一個資料夾、路徑同 `specs/`，2026-10-09）；前端用 Vitest 做單元與元件測試，瀏覽器層只對少數關鍵流程做 Playwright 冒煙測試，前端不另建 Gherkin 執行層（2026-10-09 使用者決定，docs/FRONTEND.md §8） | 每條 AC 要 E2E（UDS 測試層級選項 `e2e-testing`） |
| 文件語言與檔名 | 繁中；`docs/` 檔名維持大寫 | 英文、小寫檔名 |
| 決策紀錄（ADR） | 一個決策一檔 `ADR-NNN-短描述.md`，放在對應規格的資料夾；跨選單的放 `specs/其他/架構決策/`；編號全專案共用，索引在 specs/README.md | 集中在 `docs/adr/` |
| 規格格式與位置 | 選單分層資料夾內的 `spec.md`（見 specs/README.md） | `proposal.md`、`specs/SPEC-XXX.md` |
| CHANGELOG、LICENSE | 不維護（進度見 specs/README.md）；`docs/` 規範文件改標示版本標頭（2026-10-08，見 docs/COMMENTS.md §8） | 開發時更新 CHANGELOG、必備 LICENSE |
| 環境分支 | 允許「只在開發環境開放」的分支（Swagger、CORS、種子、本機不重導 HTTPS） | 禁止環境專屬程式路徑 |
| 登入工作階段 | JWT Bearer：access token 15 分鐘、前端存記憶體；refresh token 以 HttpOnly Cookie（SameSite=Lax）傳遞、8 小時滑動、可撤銷（2026-10-08 使用者決定，SPEC-004 FR-021、ADR-011） | Cookie SameSite=Strict、閒置 15～30 分鐘 |
| 資料庫細節 | append-only 日誌表可用邏輯關聯取代外鍵；稽核由 EF Core interceptor 寫入；關鍵字查詢用 `Contains`；停機部署，不強制 expand-contract | 資料庫層一律強制外鍵、trigger 寫稽核、全文檢索、expand-contract |
| 請求日誌 | 請求完成時記一筆（含 source_ip） | 收到請求時先記一筆 |
| 安全細節 | 密碼 PBKDF2 即可；SAST 與相依套件掃描在合併前；後端 API 不要求完整 CSP（前台、後台輸出 HTML 要設，隨部署決定，DEF-023）；秘密輪替與 Vault 不強制；不做全站 IP 層節流（只對登入端點限速）；DAST 暫不執行（無 staging） | Argon2id／bcrypt／scrypt、commit／push 前掃描、完整 CSP、90 天輪替、依使用者限速、staging DAST |
| 測試細節 | 純接線免測試先行、功能層可批次紅綠；CsCheck 隨機屬性測試（印 seed 可重現）；`/sdd discuss` 取代三方會議；寫不成 Gherkin 的 AC 改用功能或整合測試；功能測試替換驗證但授權真實；功能測試每類別一個資料庫；變異分數不設門檻；不採三路徑、ATDD 簽核、TESTPLAN、共享狀態流程測試、`@ac`（docs/TESTING.md）；覆蓋率人工比對（自動化暫不做，DEF-018） | red-first 無例外、FIRST 不得隨機、Three Amigos、AC 與情境 1:1、不替換安全控制、測試間 rollback、變異分數門檻、門檻 fail-closed |
| 建置與 CI | Monorepo 的 src、tests 放在 `backend/` 下，前端放在 `frontend/`（pnpm workspace，2026-10-09）；有改前端時合併前另跑前端 lint、typecheck、test；尚無可用 CI（`.github/workflows/ci.yml` 已備妥，但公司 GitHub Enterprise 停用 hosted runner，2026-10-08 起於 GitHub 停用此 workflow，恢復用 `gh workflow enable CI`），合併前作者在本機跑 build＋四層測試（含驗收） | 根目錄 src／tests、合併前 CI 綠燈 |
| Git 細節 | 流程選擇記在 `.github/COMMIT_CONVENTION.md` 與 docs/INFRASTRUCTURE.md（不另建 CONTRIBUTING.md）；scope 用 SPEC 編號或模組名（可中文） | CONTRIBUTING.md、scope 小寫英文、commit 語言全部一致 |
| 規格維護 | specs/README.md 索引手動維護；延後事項只登記明確延後或待決策者 | 可推導欄位自動生成、依文件結構位置登記延後項 |

> 2026-10-06 改採 UDS 流程：覆蓋率（不得下降）、Mock 邊界兩項現行 UDS 已與專案一致，移出本表；合併策略（squash）、每個 commit 全綠、繁中 commit 類型、`feature/` 分支、專案決策記憶、ADR、規格歸檔（Archived）、BDD 層改照 UDS。

> 2026-10-08 開發規範裁決（74 題，對照與勾選表見 `docs/STANDARDS-COMPARISON.html`、`docs/STANDARDS-DECISIONS.html`）：改採 UDS 12 題——錯誤回應全面 ProblemDetails、Vogen 主鍵改 IDENTITY、金額 `decimal(19,4)`、秘密掃描提前到 commit、生產日誌改 JSON 並依等級保留、變異測試改定期執行、大量資料用 keyset（同日改為原則不用，見分頁列）、撤銷 SHA-256 密碼例外、`docs/` 版本標頭、SemVer 與 `/version`；以上改採項目均已落實於範本骨架（覆蓋率自動化暫不做，DEF-018）。其餘照專案的項目補入上表。

### AI 行為約定
- 每個開發階段完成、建置成功且測試全綠後**自動 commit**（2026-10-06 使用者決定，取代 UDS 預設的「只提示、同意才 commit」）；測試未過不得 commit。
- 合併流程（UDS GitHub Flow）：工作完成後自動 push 分支、開 PR，跑 `code-review` 與 `checkin` 沒有 BLOCKING 就以 squash 合併並刪除分支（2026-10-08 使用者決定，取代「push 與開 PR 先問」）；有 BLOCKING、測試未過或需要使用者決定的事項時停下來問。
- 專案決策記憶（UDS project-context-memory）：對話中出現「之後都…」「就這樣定」等共識時，提議記錄到 `.project-context/`，**使用者同意才存**；開發者經驗（踩雷、解法）記在 `.memory/`（UDS developer-memory）。規格的 Clarifications 照舊記錄該規格的釐清結果。
- commit 訊息不加 AI 共同作者尾行（Co-Authored-By）。
- 不擅自新增規格沒有要求的功能；遇到 UDS 檢查清單要求的新功能（MFA 等），列為待決事項而不是直接做。

## Build & Test

```powershell
dotnet build TcmClinic.Api.slnx
dotnet run --project apphost/TcmClinic.Api.AspireHost        # 啟動全系統（需 Docker）
# 測試用 dotnet run --project（dotnet test 會失敗）；整合、功能、驗收測試需 Docker（依序跑，不可平行）；只跑某類別加 -- -class "完整類別名"；後端執行中加 --artifacts-path <暫存目錄>
dotnet run --project backend/tests/TcmClinic.Api.UnitTests
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests
dotnet run --project backend/tests/TcmClinic.Api.AcceptanceTests

# 前端（在 frontend/ 執行；需 Node 24＋pnpm，見 docs/FRONTEND.md §10）
pnpm install --frozen-lockfile
pnpm lint; pnpm typecheck; pnpm test
pnpm dev:site                                                   # 前台 http://localhost:3000
pnpm dev:admin                                                  # 後台 http://localhost:5173
pnpm gen:api                                                    # 後端執行中時，由 OpenAPI 重新產生 API 型別
```

<!-- UDS:STANDARDS:START -->
<!-- WARNING: This block is managed by UDS (universal-dev-standards). DO NOT manually edit. Use 'npx uds init' or 'npx uds update' to modify. -->
> **這個區塊是索引，不是標準本文。** 規則並未複製於此。
> 在依照下方任何一項行動之前，請打開 `.standards/` 底下對應的檔案並遵守其內容。
> 只憑這個區塊工作，等同於沒有採用標準。

## Installed Standards

All standards are in `.standards/`. Installed standards:

- `.standards/acceptance-criteria-traceability.ai.yaml` — acceptance-criteria-traceability
- `.standards/acceptance-test-driven-development.ai.yaml` — acceptance-test-driven-development
- `.standards/accessibility-standards.ai.yaml` — accessibility-standards
- `.standards/adr-standards.ai.yaml` — adr-standards
- `.standards/agent-dispatch.ai.yaml` — agent-dispatch
- `.standards/ai-agreement-standards.ai.yaml` — ai-agreement-standards
- `.standards/ai-command-behavior.ai.yaml` — ai-command-behavior
- `.standards/ai-friendly-architecture.ai.yaml` — ai-friendly-architecture
- `.standards/ai-instruction-standards.ai.yaml` — ai-instruction-standards
- `.standards/ai-response-navigation.ai.yaml` — ai-response-navigation
- `.standards/anti-hallucination.ai.yaml` — anti-hallucination
- `.standards/api-design-standards.ai.yaml` — api-design-standards
- `.standards/behavior-driven-development.ai.yaml` — behavior-driven-development
- `.standards/behavior-snapshot.ai.yaml` — behavior-snapshot
- `.standards/changelog.ai.yaml` — changelog
- `.standards/checkin-standards.ai.yaml` — checkin-standards
- `.standards/class-level-fix.ai.yaml` — class-level-fix
- `.standards/code-review.ai.yaml` — code-review
- `.standards/commit-message.ai.yaml` — commit-message
- `.standards/context-aware-loading.ai.yaml` — context-aware-loading
- `.standards/database-standards.ai.yaml` — database-standards
- `.standards/deferred-item-exit.ai.yaml` — deferred-item-exit
- `.standards/deployment-standards.ai.yaml` — deployment-standards
- `.standards/developer-memory.ai.yaml` — developer-memory
- `.standards/documentation-lifecycle.ai.yaml` — documentation-lifecycle
- `.standards/documentation-structure.ai.yaml` — documentation-structure
- `.standards/documentation-writing-standards.ai.yaml` — documentation-writing-standards
- `.standards/error-codes.ai.yaml` — error-codes
- `.standards/feature-discovery-standards.ai.yaml` — feature-discovery-standards
- `.standards/feature-manifest-standard.ai.yaml` — feature-manifest-standard
- `.standards/flow-based-testing.ai.yaml` — flow-based-testing
- `.standards/forward-derivation-standards.ai.yaml` — forward-derivation-standards
- `.standards/full-coverage-testing.ai.yaml` — full-coverage-testing
- `.standards/git-workflow.ai.yaml` — git-workflow
- `.standards/git-worktree.ai.yaml` — git-worktree
- `.standards/knowledge-graph-memory.ai.yaml` — knowledge-graph-memory
- `.standards/logging.ai.yaml` — logging
- `.standards/mock-boundary.ai.yaml` — mock-boundary
- `.standards/model-selection.ai.yaml` — model-selection
- `.standards/mutation-testing.ai.yaml` — mutation-testing
- `.standards/open-work-tracking.ai.yaml` — open-work-tracking
- `.standards/packaging-standards.ai.yaml` — packaging-standards
- `.standards/performance-standards.ai.yaml` — performance-standards
- `.standards/project-context-memory.ai.yaml` — project-context-memory
- `.standards/project-structure.ai.yaml` — project-structure
- `.standards/push-standards.ai.yaml` — push-standards
- `.standards/refactoring-standards.ai.yaml` — refactoring-standards
- `.standards/requirement-engineering.ai.yaml` — requirement-engineering
- `.standards/retrospective-standards.ai.yaml` — retrospective-standards
- `.standards/reverse-engineering-standards.ai.yaml` — reverse-engineering-standards
- `.standards/security-standards.ai.yaml` — security-standards
- `.standards/security-testing.ai.yaml` — security-testing
- `.standards/self-review-protocol.ai.yaml` — self-review-protocol
- `.standards/spec-driven-development.ai.yaml` — spec-driven-development
- `.standards/structured-task-definition.ai.yaml` — structured-task-definition
- `.standards/systematic-debugging.ai.yaml` — systematic-debugging
- `.standards/test-completeness-dimensions.ai.yaml` — test-completeness-dimensions
- `.standards/test-driven-development.ai.yaml` — test-driven-development
- `.standards/test-governance.ai.yaml` — test-governance
- `.standards/testing.ai.yaml` — testing
- `.standards/user-journey-testing.ai.yaml` — user-journey-testing
- `.standards/verification-evidence.ai.yaml` — verification-evidence
- `.standards/versioning.ai.yaml` — versioning
- `.standards/virtual-organization-standards.ai.yaml` — virtual-organization-standards
- `.standards/options/traditional-chinese.ai.yaml` — traditional-chinese
- `.standards/options/github-flow.ai.yaml` — github-flow
- `.standards/options/squash-merge.ai.yaml` — squash-merge
- `.standards/options/unit-testing.ai.yaml` — unit-testing
- `.standards/options/integration-testing.ai.yaml` — integration-testing
- `.standards/options/system-testing.ai.yaml` — system-testing
- `.standards/options/e2e-testing.ai.yaml` — e2e-testing
<!-- UDS:STANDARDS:END -->
