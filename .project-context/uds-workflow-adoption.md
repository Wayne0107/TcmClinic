---
id: PRJ-2026-0001
type: workflow
status: active
confidence: 1.0
scope:
  - "specs/**"
  - "backend/**"
  - ".github/COMMIT_CONVENTION.md"
  - "AGENTS.md"
  - "CLAUDE.md"
triggers:
  - "commit"
  - "合併"
  - "分支"
  - "ADR"
  - "決策紀錄"
  - "BDD"
  - "歸檔"
  - "開發流程"
created: 2026-10-06
---

# 開發流程採 UDS（憲章 2.1.0）

## Context

源專案原本在 UDS 之上另有多項裁決（紅燈 commit、`--no-ff` 合併、英文 commit 類型、`NNN-功能名` 分支、不建 ADR、不用專案決策記憶、不設 BDD 層）。2026-10-06 維護者決定開發流程改照原版 UDS，只保留少數例外；範本沿用此流程。

## Decision/Rule

- 每個 commit 都必須建置成功、測試全綠；TDD 紅燈只在本機驗證，執行摘要記在規格的「實作紀錄」。
- commit 類型用繁中（`新增`、`修正`、`文件`、`測試`…，UDS traditional-chinese 選項）。
- 分支 `feature/NNN-短名`（修 bug 用 `fix/NNN-短名`）。
- 分支 push 到 GitHub 開 PR，合併前跑 `code-review` 與 `checkin`，以 squash 合併。
- 新規格核准後以 `derive-bdd` 產出 Gherkin `.feature`（Reqnroll 執行）。
- 合併後 `sdd archive`，規格狀態改為 Archived。
- 專案決策共識記到 `.project-context/`，開發者經驗記到 `.memory/`，都要使用者同意才存。
- AI 在每個階段完成且建置成功、測試全綠後自動 commit（2026-10-06 使用者決定，取代原本「只提示、同意才 commit」）。工作完成後自動 push 分支、開 PR，跑 `code-review` 與 `checkin` 沒有 BLOCKING 就以 squash 合併並刪除分支（2026-10-08 使用者決定，取代「push 與開 PR 先問」）；有 BLOCKING、測試未過或需要使用者決定的事項時停下來問。
- ADR 一個決策一檔 `ADR-NNN-短描述.md`，放在對應規格資料夾；跨選單的放 `specs/其他/架構決策/`；索引在 `specs/README.md`。

## Constraint

- 維持不採用：E2E（瀏覽器）層、CHANGELOG、`specs/SPEC-XXX.md` 扁平位置、`docs/` 小寫檔名，以及 API／資料庫設計類裁決（見 AGENTS.md「已裁決的衝突」）。
- 範本內建規格裡的「設計決策 R1～Rn」維持在規格內，不搬成 ADR。
- 詳細流程見 `docs/WORKFLOW.html`。
