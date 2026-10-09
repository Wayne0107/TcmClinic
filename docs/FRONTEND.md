# 前端規範（Frontend）

> 版本 1.0.0｜最後更新 2026-10-09

> 本檔為憲章原則 I～VII 在前端（`frontend/`）的 **How**：前台（Nuxt 4）、後台（Vue 3）與共用套件。
> 後端專屬的規則（Clean Architecture 分層、Vogen、EF Core、xUnit）不適用前端；前端沒有規定的事項，依憲章裁決，再參考 `.standards/`。

## 1. 範圍與技術棧

| 子專案 | 路徑 | 套件名稱 | 用途 | 技術 |
|--------|------|---------|------|------|
| 前台 | `frontend/site/` | `@tcm-clinic/site` | 對外公開網站：診所介紹、服務、Blog、預約；需要 SEO | Nuxt 4（SSR）、Vue 3、TypeScript |
| 後台 | `frontend/admin/` | `@tcm-clinic/admin` | 診所人員使用的管理介面；登入後依選單授權操作 | Vue 3＋Vite（SPA）、Vuetify 4、Pinia、Vue Router、TypeScript |
| 共用 | `frontend/shared/` | `@tcm-clinic/shared` | 兩端共用的 API 型別（由後端 OpenAPI 產生）與不依賴框架的純函式 | TypeScript |

- **套件管理**：pnpm workspace（`frontend/pnpm-workspace.yaml`），版本固定在 `frontend/package.json` 的 `packageManager`。不得混用 npm、yarn。
- **Node.js**：24 LTS 以上（`frontend/package.json` 的 `engines`）。
- **TypeScript**：固定 6.0.x。TypeScript 7（原生編譯器）待 `vue-tsc` 確認相容後再升級。
- **選型理由**：
  - 前台用 Nuxt：舊站靠在地 SEO 取得預約，頁面必須由伺服器端渲染完整 HTML。Nuxt 3 已於 2026-07-31 停止維護，因此用 Nuxt 4（憲章原則 I）。
  - 後台用 Vue 3＋Vuetify：與公司既有後台前端一致，元件齊全，適合表單與清單為主的管理介面。

## 2. 目錄結構

```
frontend/
├── package.json            # workspace 根：共用腳本（lint、typecheck、test、gen:api）、packageManager、engines
├── pnpm-workspace.yaml
├── pnpm-lock.yaml          # 進版控；安裝一律 --frozen-lockfile
├── .editorconfig
├── .prettierrc.json
├── shared/
│   └── src/
│       ├── api/schema.d.ts # 由後端 OpenAPI 產生，禁止手改（§5.2）
│       └── index.ts
├── site/                   # Nuxt 4（app/ 目錄結構）
│   ├── nuxt.config.ts
│   ├── app/
│   │   ├── app.vue
│   │   ├── pages/          # 檔案即路由（kebab-case）
│   │   ├── layouts/
│   │   ├── components/
│   │   ├── composables/
│   │   └── assets/
│   ├── public/
│   └── test/
└── admin/                  # Vue 3 + Vite
    ├── index.html
    ├── vite.config.ts
    └── src/
        ├── main.ts
        ├── App.vue
        ├── router/
        ├── plugins/        # Vuetify、Pinia 等接線
        ├── layouts/
        ├── features/       # 功能優先：一個選單功能一個資料夾
        │   └── <功能>/     #   views/、components/、composables/、api.ts、*.test.ts
        └── shared/         # 後台內跨功能共用：http client、auth store、通用元件
```

- **功能優先**（與後端 `docs/CODING-STANDARDS.md` §5 一致）：後台每個選單功能一個 `features/<功能>/` 資料夾，資料夾名稱用英文 kebab-case（如 `features/booking-records`），對應的規格寫在檔頭註解。
- 前台依 Nuxt 慣例：`pages/` 的檔案路徑就是網址，路徑必須與規格的 URL 一致（舊站網址要保留，以免流失搜尋排名）。
- 測試檔與被測檔放在同一個資料夾，命名 `<檔名>.test.ts`。Nuxt 執行環境的測試放 `site/test/`。

## 3. 分層與相依方向（憲章原則 III）

```
頁面／視圖（pages、views）
   ↓
元件（components）     ← 只負責顯示與發出事件
   ↓
composable／store      ← 狀態、流程、呼叫 API
   ↓
API 模組（api.ts、shared/http）
   ↓
@tcm-clinic/shared     ← 型別與純函式，不依賴 Vue／Nuxt
```

- 依賴只能往下。**元件不得直接呼叫 `fetch`、`$fetch` 或 API client**，一律經由 composable 或 store。
- `@tcm-clinic/shared` 不得 import `vue`、`nuxt`、`vuetify` 或任何瀏覽器全域物件，比照後端 Core 不依賴任何框架。
- 一個功能不得 import 另一個功能 `features/<其他>/` 的內部檔案。需要共用時，移到 `src/shared/`。
- 以上規則由 ESLint 的 `no-restricted-imports` 強制，違反就是 lint 錯誤，比照後端用 NetArchTest 做架構測試。
- **業務規則以後端為準**：前端只做顯示與互動，不重新實作後端的業務判斷。前端驗證只為提升體驗，送出後一律以後端回應為準。

## 4. 編碼慣例（憲章原則 IV、V）

### 4.1 型別安全（MUST）

- `tsconfig` 開啟 `strict`、`noUncheckedIndexedAccess`、`noImplicitOverride`。型別檢查用 `vue-tsc --noEmit`（後台）、`nuxi typecheck`（前台），錯誤一律擋下。
- **禁止 `any`**（ESLint `@typescript-eslint/no-explicit-any` 為 error）。外部資料型別不明時用 `unknown`，再以型別守衛縮小範圍。
- **禁止 `@ts-ignore`、`@ts-nocheck`**。無法避免時用 `@ts-expect-error`，並在同一行寫出理由。
- 非空斷言 `!` 只在型別系統無法推論、但邏輯上已確定時使用，並加註解說明為何必定有值。
- API 的請求與回應型別一律取自 `@tcm-clinic/shared` 產生的型別（§5.2），**不得手寫一份平行的 DTO 介面**。
- 識別碼是後端的 UUID 字串（`PublicId`），前端視為不透明值，只負責往返傳遞，不解析、不組合，也不另外做 branded type（前端沒有領域邏輯，強型別由後端負責）。

### 4.2 Vue 寫法（MUST）

- 一律用 `<script setup lang="ts">` 與 Composition API，不用 Options API。
- SFC 區塊順序：`<script setup>` → `<template>` → `<style scoped>`。
- `defineProps`、`defineEmits` 用型別宣告（`defineProps<{ … }>()`）；選填 props 以 `withDefaults` 給預設值。
- 父子元件溝通用 props 往下、事件往上（或 `defineModel`），不得直接修改 props。
- `v-for` 必須有穩定的 `:key`（用識別碼，不用陣列索引）。不得在同一個元素上同時用 `v-if` 和 `v-for`。
- 樣式預設 `scoped`。全站共用的樣式與設計 token 放在 `assets/`（前台）或 Vuetify theme（後台）。

### 4.3 命名

| 元素 | 規則 | 範例 |
|------|------|------|
| 元件檔與元件名 | PascalCase、至少兩個字 | `BookingForm.vue`、`DoctorCard.vue` |
| composable | `use` 前綴、camelCase | `useBookingForm.ts` |
| Pinia store | `use<名詞>Store`，檔名為名詞 | `stores/auth.ts` → `useAuthStore` |
| 其他 TS 檔 | kebab-case | `problem-details.ts` |
| Nuxt 頁面 | kebab-case；動態段用 `[param]` | `pages/blog/[slug].vue` |
| 型別、介面 | PascalCase | `BookingRequest` |
| 常數 | UPPER_SNAKE_CASE | `DEFAULT_PAGE_SIZE` |
| 事件名 | camelCase 宣告，模板中寫 kebab-case | `emit('submitted')` |

### 4.4 格式與檢查

- 格式以 `frontend/.editorconfig`（2 空格、LF、UTF-8）與 Prettier（`frontend/.prettierrc.json`）為準，ESLint 只負責程式碼品質，不檢查格式。
- ESLint 用 flat config：前台用 `@nuxt/eslint`，後台用 `eslint-plugin-vue`＋`typescript-eslint`。規則寫在各子專案的 `eslint.config.*`。
- 提交前 `pnpm lint`、`pnpm typecheck`、`pnpm test` 必須全綠（§10）。
- **禁止提交除錯程式碼**：`console.log`、`debugger` 為 lint 錯誤。

### 4.5 註解（憲章原則 VII）

- 註解一律用繁體中文，識別字用英文（`docs/COMMENTS.md` §0）。
- 文件式註解用 TSDoc（`/** … */`），用在 `@tcm-clinic/shared` 匯出的函式與型別，以及跨功能共用的 composable：說明用途、參數、回傳值與邊界情況。
- 規格式註解（為何這樣寫、業務限制）引用規格編號，例如 `// SPEC-066 FR-003：預約日期不得早於今日`。
- 每個功能資料夾的進入點（view 或 page）在檔頭註明對應的規格：`<!-- SPEC-066 網上預約 -->`。

## 5. 與後端 API 串接（憲章原則 IV、V）

### 5.1 呼叫路徑

- 前端一律以相對路徑 `/api/...` 呼叫後端，由代理轉給後端並去掉 `/api` 前綴。後端路由本身沒有 `/api` 前綴，例如 `/api/auth/login` 會轉到 `/auth/login`。
  - 後台開發：Vite dev server 的 `server.proxy`。
  - 前台：Nitro 的 `routeRules` proxy，SSR 與瀏覽器端共用同一個路徑。
  - 代理目標預設 `http://localhost:5143`（後端 `launchSettings.json` 的 http 位址），可用環境變數 `API_PROXY_TARGET` 覆寫。
- 前端與後端維持**同源**（經過代理），所以 refresh token 的 HttpOnly Cookie（`SameSite=Lax`、`Path=/`）會自動帶上，不需要額外設定 CORS。
- 正式環境的代理與託管方式尚未決定（specs/README.md 延後事項 DEF-023）。

### 5.2 API 型別與 client

- 型別由後端 OpenAPI 文件產生：後端以 Development 啟動後，在 `frontend/` 執行 `pnpm gen:api`。它會讀取 `http://localhost:5143/swagger/v1/swagger.json`，以 `openapi-typescript` 輸出到 `shared/src/api/schema.d.ts`。產生的檔案進版控，**禁止手改**。
- 後端契約變更（新增、修改端點或 DTO）時，同一個規格的工作內要重新產生型別，並修正型別檢查錯誤。前端用 `vue-tsc` 和 `nuxi typecheck` 擋下契約漂移。
- 呼叫 API 一律用 `openapi-fetch` 建立的型別化 client（路徑、參數、body、回應都受型別檢查），不直接用 `fetch`、`axios`。
  - 後台：`src/shared/http.ts` 建立唯一的 client，並以 middleware 處理 Bearer token 與 401 換新（§6.2）。
  - 前台：在 Nuxt plugin 建立 client，頁面資料以 `useAsyncData(() => client.GET(...))` 取得，SSR 的資料才會隨 HTML 輸出、不在瀏覽器重抓。

### 5.3 錯誤處理（MUST）

後端所有錯誤都是 RFC 9457 ProblemDetails（`docs/API-CONVENTIONS.md` §3）。前端用一套共用的轉換函式（放在 `@tcm-clinic/shared`）處理，各畫面不得自行解析錯誤格式：

| 狀態碼 | 前端處理 |
|--------|---------|
| 400 ValidationProblem | `errors` 的鍵（camelCase）對應到表單欄位並顯示在欄位下方；鍵名 `error` 顯示為表單層級訊息 |
| 401 | 後台：先自動換新 access token 並重試一次（§6.2），仍然 401 就導向登入頁 |
| 403 | 顯示「沒有權限」頁面，不重試 |
| 404 | 顯示找不到資料；前台頁面回傳真正的 404 狀態（`createError({ statusCode: 404 })`），不可以 200 回應錯誤頁 |
| 409 | 提示「資料已被其他人修改，請重新整理後再試」，不自動覆蓋 |
| 429 | 讀取 `Retry-After`，顯示需等待的秒數 |
| 500 與網路錯誤 | 顯示通用錯誤訊息並附上 `traceId`，方便對照後端日誌；不顯示例外細節 |

### 5.4 分頁

- 清單參數用 `page`、`pageSize`；回應是 `{ items, page, pageSize, totalCount }`（ADR-007）。前端不另外假設預設值，未指定時由後端決定（預設 10、上限 100）。
- 後台清單用 Vuetify `v-data-table-server`，換頁時以 `page`、`pageSize` 重新查詢，不一次載入全部資料。

## 6. 狀態管理與登入（後台）

### 6.1 狀態

- 跨頁共用的狀態用 Pinia store（setup store 寫法），只放在一個功能內的狀態留在 composable。
- 伺服器資料以 API 為準，store 不另外保存一份需要手動同步的副本。

### 6.2 登入工作階段（MUST，對應 ADR-011）

- **access token 只存在記憶體**（auth store 的 state），不得寫入 `localStorage`、`sessionStorage`、IndexedDB 或非 HttpOnly 的 Cookie。
- refresh token 由後端以 HttpOnly Cookie 發給瀏覽器，前端 JavaScript 讀不到也不處理。
- 每個受保護請求都帶 `Authorization: Bearer <access token>`。
- 收到 401 時呼叫 `POST /auth/refresh` 換新 token，成功就重試原請求一次。多個請求同時遇到 401 時，共用同一個換新請求（single-flight），不得同時發出多個 refresh。
- 重新整理頁面後，記憶體中的 token 會消失：App 啟動時先呼叫 `POST /auth/refresh` 嘗試恢復登入，失敗就導向登入頁。
- 登出呼叫 `POST /auth/logout`，並清除 store 中的 token 與使用者資料。

### 6.3 選單與路由授權

- 選單取自 `GET /me/menus`（`{ text, value, subMenu? }`，SPEC-008）。路由的 `meta.menu` 對應選單的 `value`，router guard 只放行使用者選單中有的路由。
- **前端隱藏選單和擋路由只是使用者體驗，不是存取控制**（`docs/SECURE-CODING.md` §4）。授權一律由後端判定，前端遇到 403 依 §5.3 處理。

## 7. 安全（憲章原則 I，MUST）

- **XSS**：
  - 預設用模板插值輸出。`v-html` 是 lint 錯誤（`vue/no-v-html`）。
  - 必須輸出 HTML 時（例如 Blog 文章內文），內容要先以 DOMPurify 消毒，並在那一行用 `eslint-disable-next-line vue/no-v-html` 加註理由。
  - 後端在儲存時同樣要驗證或消毒，前端消毒不能取代後端。
- **秘密**：前端程式碼與建置產物都會公開，不得包含任何秘密（API key、簽章金鑰、連線字串）。
  - 只有公開值可以放進 `VITE_*`、`NUXT_PUBLIC_*`。
  - Nuxt 的 `runtimeConfig` 非 public 欄位只在伺服器端使用。
- **Token 存放**：依 §6.2。
- **外部連結**：`target="_blank"` 一律加 `rel="noopener noreferrer"`。
- **表單防濫用**：公開表單（例如預約）的防機器人措施（蜜罐欄位、限速等）依該表單的規格實作，並以後端判定為準。
- **安全標頭**：前台與後台輸出 HTML，正式環境要設定 CSP、`X-Content-Type-Options`、`Referrer-Policy`，並禁止被嵌入框架。由哪一層設定隨部署方式決定（DEF-023）。
- **相依弱點**：合併前執行 `pnpm audit --prod`，High／Critical 必須修正或升級才能合併（`docs/SECURE-CODING.md` §8）。
- **個資**：預約等表單的個資不得寫入 `console`、URL 查詢參數或前端錯誤回報。

## 8. 測試（憲章原則 II）

### 8.1 層級與工具

| 層級 | 對象 | 工具 | 測試先行 |
|------|------|------|---------|
| 單元 | `@tcm-clinic/shared` 純函式、composable、Pinia store | Vitest | MUST（嚴格 Red-Green-Refactor） |
| 元件 | 有分支邏輯或互動的元件（表單驗證、條件顯示、事件） | Vitest＋`@vue/test-utils`（happy-dom）；前台需要 Nuxt 執行環境時用 `@nuxt/test-utils` | MUST |
| 冒煙（E2E） | 少數關鍵使用者流程：後台登入、前台送出預約 | Playwright，對真實後端（Aspire 啟動） | SHOULD；有關鍵流程時才引入 |

- **明確排除（不需測試先行）**：純展示、沒有分支邏輯的元件；版面與樣式；路由表、plugin 註冊、`nuxt.config.ts`、`vite.config.ts` 等接線；自動產生的 `schema.d.ts`。加入邏輯後就回到測試先行。
- **不做每條驗收條件的 E2E**。前端規格的驗收條件以元件或單元測試驗證，冒煙測試只確認關鍵流程能從頭走到尾。
- **Gherkin**：後端以 Reqnroll 執行驗收情境（`docs/TESTING.md` §4.6）。前端不另外建 Gherkin 執行層，前端行為的驗收條件寫不成後端情境時，改用元件測試並加 `@SPEC/@AC` 標記。

### 8.2 Mock 邊界

- API 一律在網路邊界用 MSW 模擬，不 mock 自家的 composable、store 或 API 模組。測試要經過真實的 client、錯誤轉換與狀態流程。
- 新寫的 mock 前一行加 `// MOCK: <理由>`（與後端 `docs/TESTING.md` §2 一致）。
- 冒煙測試不 mock，直接打真實後端與測試資料庫。

### 8.3 命名、意圖註解與追溯（MUST）

- `describe` 用被測對象名稱，`it`／`test` 直接寫繁體中文的預期行為（作用等同後端的 `DisplayName`）。
- 每個測試前加一段意圖註解，說明情境與預期。
- 驗證規格驗收條件的測試加 `// @SPEC-NNN @AC-N`。

```ts
// 情境：後端回 400 ValidationProblem → 欄位錯誤應對應到表單欄位，不顯示通用錯誤。
// @SPEC-066 @AC-4
it('送出失敗時把欄位錯誤顯示在對應欄位下方', async () => { /* ... */ })
```

- 空斷言、恆真測試、只檢查「有沒有 render」的測試，審查時退回（`docs/TESTING.md` §4.5）。

### 8.4 覆蓋率

- 以 `@vitest/coverage-v8` 量測（`pnpm test:coverage`），**覆蓋率不得下降**，比對方式同後端（人工比對，DEF-018）。
- 不設數值門檻。

## 9. 效能（憲章原則 VI）

- **前台**：以行動版 Lighthouse 量測 Core Web Vitals，目標 **LCP < 2.5s、CLS < 0.1、INP < 200ms**，首頁總傳輸量（不含影片）**< 1MB**（沿用舊站分析的新版目標，`oldSpec/07-non-functional/performance.md`）。
  - 圖片提供 `width`、`height`，首屏以外的圖片 lazy 載入，首屏主圖 preload。
  - 中文字型要做子集化或減少字重。
  - 公開頁面不得發出 session Cookie，讓 HTML 可以被快取。
- **後台**：
  - 路由元件一律動態載入（`() => import(...)`），Vuetify 元件按需載入（`vite-plugin-vuetify` 的 autoImport）。
  - bundle 大小以第一次建置的量測值為基準，退步要在 PR 說明理由。
- 效能優化不得犧牲安全、可測試性或型別安全（憲章原則 VI）。

## 10. 指令

在 `frontend/` 執行：

```powershell
pnpm install --frozen-lockfile   # 安裝（第一次或 lockfile 變更後）
pnpm dev:site                    # 前台 dev server（http://localhost:3000）
pnpm dev:admin                   # 後台 dev server（http://localhost:5173）
pnpm lint                        # 全部子專案 ESLint
pnpm typecheck                   # 全部子專案型別檢查
pnpm test                        # 全部子專案 Vitest
pnpm test:coverage               # 含覆蓋率
pnpm gen:api                     # 由執行中的後端產生 API 型別（§5.2）
```

- 後端用 `dotnet run --project apphost/TcmClinic.Api.AspireHost` 啟動，再啟動前端 dev server。
- **合併前**：前端的 `lint`、`typecheck`、`test` 全綠，加上後端建置與四層測試（`docs/INFRASTRUCTURE.md` §4）。只改前端時仍要跑前端三項，只改後端時不必跑前端。

## 11. 相依套件

- 新增、升級、移除套件時，在**同一次變更**更新 `THIRD-PARTY-NOTICES.md`，宣告位置寫該子專案的 `package.json`，授權政策同 `docs/DEPENDENCIES.md` §2。
- 版本範圍用 `^`，實際版本以 `pnpm-lock.yaml` 為準。安裝一律 `--frozen-lockfile`，不得只改 `package.json` 而不提交 lockfile。
- 共用的開發工具（Prettier、TypeScript）放在 workspace 根的 `devDependencies`。各子專案的執行期相依放在該子專案。
- 授權檢查：`pnpm licenses list --prod`。

## 12. 無障礙與 SEO

- **無障礙**：以 WCAG 2.1 AA 為目標。
  - 用語意化 HTML（每頁一個 `h1`、`nav`、`main`、`button` 不用 `div` 代替）。
  - 表單欄位都要有 `<label>`，所有互動都能用鍵盤操作。
  - 尊重 `prefers-reduced-motion`；自動播放的輪播要能暫停。
  - 圖片要有有意義的 `alt`，裝飾性圖片用空字串。
  - 舊站已知問題見 `oldSpec/07-non-functional/accessibility.md`，新版不得重犯。
- **SEO（前台）**：
  - 每個頁面以 `useSeoMeta` 設定 `title`、`description` 與 Open Graph，並輸出 canonical。
  - 結構化資料（JSON-LD）、sitemap、robots 依前台規格實作。
  - `<html lang>` 依前台規格的語系設定。

## 13. 規格

- 後台功能照後端的規則，依選單放在 `specs/<業務>/<作業>/<功能>/spec.md`。前後端在同一份規格內描述，API 契約與畫面行為寫在一起。
- 前台沒有選單，規格放在 `specs/前台/<頁面或功能>/spec.md`，例如 `specs/前台/網上預約/spec.md`。
- `oldSpec/` 是舊站的逆向分析，只是撰寫新規格時的參考資料，不是規格本身。新功能仍要經過 SDD 流程寫成 `specs/` 的規格。
