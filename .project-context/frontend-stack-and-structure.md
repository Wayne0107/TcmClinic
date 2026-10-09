---
id: PRJ-2026-0010
type: decision
status: active
confidence: 1.0
scope:
  - "frontend/**"
  - "docs/FRONTEND.md"
  - "docs/CONSTITUTION.md"
triggers:
  - "前端"
  - "前台"
  - "後台"
  - "Nuxt"
  - "Vue"
  - "Vuetify"
  - "pnpm"
created: 2026-10-09
---

# 前端技術選型與結構

## Context

範本只有後端；本專案由同一位開發者一併撰寫前端：前台是對外的診所網站（靠在地 SEO 取得預約），後台是診所人員的管理介面。2026-10-09 使用者確認以下選型（憲章 2.2.0）。

## Decision/Rule

- 同一個 monorepo：前端放 `frontend/`（pnpm workspace），子專案 `site`、`admin`、`shared`。
- 前台用 **Nuxt 4**（SSR），不用 Nuxt 3：Nuxt 3 已於 2026-07-31 停止維護。
- 後台用 **Vue 3＋Vite＋Vuetify＋Pinia**：與公司既有後台前端（CHB.Web）一致。
- API 型別由後端 OpenAPI 產生到 `frontend/shared`（`pnpm gen:api`），呼叫一律用 openapi-fetch；前端以 `/api/...` 經代理呼叫後端。
- TypeScript 固定 6.0.x，待 `vue-tsc` 確認支援 TypeScript 7 再升級。
- 識別碼（UUID）在前端視為不透明字串，不做 branded type。
- `docs/` 進版控，前後端共用；`.standards/`、`CLAUDE.md` 維持本機保留。

## Constraint

- 不得手寫與後端 DTO 平行的介面；不得混用 npm、yarn。
- 細節以 `docs/FRONTEND.md` 為準；正式環境部署方式待決（DEF-023）。
