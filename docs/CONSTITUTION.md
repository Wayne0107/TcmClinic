<!--
Sync Impact Report（2.1.0 → 2.2.0，MINOR，2026-10-09）
- 異動：治理範圍由後端擴及前端（前台 Nuxt 4、後台 Vue 3）。原則 III 補充前端分層；Technology Constraints 新增前端條目，測試工具加入 Vitest 與 Playwright。
- 未變更：七條原則的裁決內容與優先順序。
- 需同步：docs/FRONTEND.md（新增）、docs/INFRASTRUCTURE.md、docs/TESTING.md、docs/SECURE-CODING.md、docs/DEPENDENCIES.md、docs/PERFORMANCE.md、docs/COMMENTS.md、AGENTS.md、README.md。
-->
<!-- 本憲章自 CHB 專案 2.1.0 版沿用，新專案從此版本起算（源專案的 Sync Impact Report 歷史不隨範本保留）。 -->

# Clean Architecture Template Constitution

本憲章定義本專案（.NET Clean Architecture 後端＋Vue／Nuxt 前端）中，人類與 AI 協作的**最高裁決協議**。
它只回答「我們追求什麼、衝突時聽誰的（What & Why）」；具體「如何實作（How）」
由 `docs/` 系列文件與工具設定管理，並由本憲章引用。當效率、工具預設行為或個人偏好
與本憲章衝突時，**以本憲章為最高優先**。

## Core Principles

### I. 安全性 > 一切（NON-NEGOTIABLE）

安全性優先於效能、便利與交付速度。任何交付物在合併前必須通過安全基準（OWASP Top 10）。
密鑰、個資、憑證禁止硬編碼或提交進版本控制；授權必須在伺服器端強制且做到物件層級檢查。
**裁決**：當「趕時間／更方便」與「安全檢查」衝突時，一律選安全。How 見 `docs/SECURE-CODING.md`。

### II. 測試優先，程式碼必須可測試（NON-NEGOTIABLE）

不可測試的程式碼視為未完成。新行為必須測試先行（Red-Green-Refactor），即使緊急修復也不例外。
測試覆蓋不得因變更而下降；測試必須驗證可觀察行為，禁止空斷言。
**裁決**：當「先跑起來」與「測試優先」衝突時，一律選測試優先。How 見 `docs/TESTING.md`。

### III. 架構一致性不可違反（NON-NEGOTIABLE）

Clean Architecture 的依賴方向與分層邊界必須嚴格遵守：依賴永遠指向內層，Core 零框架相依，
內層不得引用外層。禁止引入破壞一致性的平行技術體系。
前端同樣單向依賴：頁面 → 元件 → composable／store → API 模組 → 不依賴框架的共用型別；元件不得直接呼叫 API。
**裁決**：當「少一層抽象比較快」與「職責分層清晰」衝突時，一律選分層清晰。How 見 `docs/CODING-STANDARDS.md`、`docs/FRONTEND.md`。

### IV. 類型安全 > 開發速度（NON-NEGOTIABLE）

以值物件與強類型表達業務概念，禁止裸原始型別作為領域識別。對外邊界（API、模組、狀態）
必須有明確契約；禁止以繞過型別系統的手段規避問題。即使是 POC 也適用。
**裁決**：當「快速原型」與「類型完整性」衝突時，一律選類型完整性。How 見 `docs/CODING-STANDARDS.md`、`docs/DATABASE-SCHEMA.md`。

### V. 系統一致性優於個人風格

同類操作在整個系統中必須表現一致：API 回應格式、狀態碼語意、錯誤形狀、命名語意一致。
可讀性優先於技巧性；命名必須表達業務語義，禁止只有作者能懂的隱式約定。
API 對外契約若因客戶明確需求而偏離標準，必須在程式碼中文件化理由。
How 見 `docs/API-CONVENTIONS.md`、`docs/COMMENTS.md`。

### VI. 效能是可衡量的需求，但不凌駕核心原則

效能必須以實測數據（遙測）為依據，不得基於臆測進行最佳化。每個功能宜宣告可觀察的效能預算。
效能改善**不得**以犧牲安全性、可測試性、架構一致性或類型安全為代價；應優先透過架構改善達成。
**裁決**：效能永遠排在原則 I–IV 之後；禁止以效能為由跳過檢查、放棄測試或硬編碼。
How 見 `docs/PERFORMANCE.md`；可觀測性與日誌見 `docs/LOGGING.md`。

### VII. 文件與註解使用繁體中文（zh-TW）

所有規格、計畫、任務、使用者文件、程式碼註解與 API 描述必須以繁體中文撰寫。
程式碼識別符（變數、類別、方法名）維持英文，遵循一般命名慣例。
**裁決**：當工具預設輸出英文而團隊主語言為繁中時，以繁體中文為最終交付語言。

## Technology Constraints（技術棧約束）

本專案的技術裁決以「約束＋理由」形式聲明，屬專案級決策；未經治理流程不得替換核心路線。
工具的具體用法與設定（How）見 `docs/`。

- **執行環境／API**：.NET 9+ 與 FastEndpoints（REPR）。理由：團隊經驗、輕量、一端點一檔。
- **資料存取**：Entity Framework Core。理由：.NET 生態標準、事務完整、與強類型 ID 整合良好。
- **主資料庫**：SQL Server。**禁止** MongoDB、Cassandra 等 NoSQL。
  理由：事務一致性需求高、團隊無 NoSQL 運維經驗。
- **強類型 ID**：Vogen。理由：編譯期類型安全、業界標準、EF Core 整合成熟。
- **測試**：後端用 xUnit、Testcontainers、CsCheck（屬性）、Verify（契約）、NetArchTest（架構）；
  前端用 Vitest（單元、元件）、Playwright（少數關鍵流程的冒煙測試）。理由：以機器可驗證的機制守住原則 II／III。
- **前台**：Nuxt 4（SSR）＋ TypeScript。理由：公開網站靠在地 SEO 取得預約，需要伺服器端渲染；Nuxt 3 已停止維護。
- **後台**：Vue 3 ＋ Vite（SPA）＋ Vuetify ＋ Pinia ＋ TypeScript。理由：與公司既有的後台前端一致，表單與清單元件齊全。
- **前端套件管理與 API 契約**：pnpm workspace；API 型別由後端 OpenAPI 產生，禁止手寫平行的 DTO（守住原則 IV）。
- **日誌**：Serilog（結構化，生產環境須持久化）。理由：客戶自建環境需可追溯的診斷紀錄。

新增外部相依必須說明「解決什麼問題」與「長期成本」，並記錄授權（見治理）。

## Development Workflow（開發流程與品質閘門）

- **TDD 循環（強制）**：Red（寫失敗測試，確認因邏輯缺失而失敗）→ Green（最小實作通過）
  → Refactor（測試保持綠燈）。測試先於實作的證據記在規格的「實作紀錄」（紅燈測試的執行結果）；
  每個 commit 都須建置成功、測試全綠。
- **緊急修復例外**：即使時間緊迫，仍須寫測試、遵守分層、通過安全檢查、不硬編碼密鑰。
- **合併閘門（一律經 PR、以 squash 合併至 master，合併前全數通過）**：所有測試通過；覆蓋率未下降；架構測試通過；
  安全掃描無 High/Critical；Code Review 批准（`code-review`、`checkin`）。Review 逐條對照 Core Principles，
  違反 NON-NEGOTIABLE 為 BLOCKER。
- **實作細節與檢查清單（How）**見 `docs/` 系列文件；依規格實作時（UDS 規格驅動流程，
  見 `.standards/spec-driven-development.ai.yaml`），除本憲章外，應一併參考與變更相關的 `docs/` 規範。

## Governance

本憲章為本專案最高治理協議，優先級高於所有 Rules、指南與工具預設。
所有合併與 Code Review 必須驗證合規；任何偏離必須文件化理由（PR 描述）並經維護者核可。
UDS 標準（`.standards/`）與 AI 工具的預設規則，僅在本憲章與 `docs/` 未規定時適用；衝突時依「本憲章 > `docs/` > 規格 > `.standards/`」裁決。

- **憲章 vs docs/ 的分工**：憲章定義「應當如何裁決（What）」；`docs/` 定義「如何實作（How）」。
  兩者衝突時以憲章為準。技術細節的變動只需改 `docs/`，不必動憲章，以保持憲章穩定。
- **修訂流程**：提議（動機＋影響範圍）→ 評估（對一致性與長期成本的影響）→ 維護者核可
  → 更新憲章與相關 `docs/` → 必要時提供遷移方案。
- **版本策略（SemVer）**：MAJOR＝原則移除或重新定義／裁決權變更；MINOR＝新增或實質強化原則；
  PATCH＝措辭澄清與非語意調整。
- **Sync Impact Report**：每次 MINOR／MAJOR 修訂必須在檔案頂端以 HTML 註解記錄版本變化、
  異動的原則與章節、以及需同步更新的 `docs/`／範本清單。
- **相依授權**：新增第三方相依須記錄授權（許可式優先；Copyleft／AGPL 須維護者書面核可）。
  How 見 `docs/DEPENDENCIES.md`；Monorepo 結構、Aspire 編排、新專案建立與 CI 見 `docs/INFRASTRUCTURE.md`。

**Version**: 2.2.0 | **Ratified**: 2026-05-24 | **Last Amended**: 2026-10-09
