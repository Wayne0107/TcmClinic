---
id: PRJ-2026-0011
type: convention
status: active
confidence: 1.0
scope:
  - "frontend/**/*.test.ts"
  - "docs/FRONTEND.md"
  - "docs/TESTING.md"
  - "AGENTS.md"
triggers:
  - "前端測試"
  - "E2E"
  - "Playwright"
  - "Vitest"
  - "元件測試"
created: 2026-10-09
---

# 前端測試範圍

## Context

範本原本「只涵蓋後端、不設 E2E 層」。加入前端後，2026-10-09 使用者確認前端的測試範圍。

## Decision/Rule

- 單元與元件測試用 Vitest（＋`@vue/test-utils`、`@nuxt/test-utils`）；composable、store、有分支的元件要測試先行。
- 瀏覽器層只對少數關鍵流程（後台登入、前台送出預約）做 Playwright 冒煙測試，有關鍵流程時才引入。
- 不做每條驗收條件的 E2E；前端不另建 Gherkin 執行層，前端行為的 AC 以元件測試＋`// @SPEC-NNN @AC-N` 標記驗證。
- API 在網路邊界以 MSW 模擬，不 mock 自家的 composable、store 或 API 模組。

## Constraint

- `it(...)` 用繁中描述預期行為，前一行寫測試意圖註解。
- 細節以 `docs/FRONTEND.md` §8 為準。
