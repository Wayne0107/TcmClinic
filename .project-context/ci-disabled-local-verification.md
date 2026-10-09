---
id: PRJ-2026-0009
type: convention
status: active
confidence: 1.0
scope:
  - ".github/workflows/**"
  - "AGENTS.md"
  - "docs/INFRASTRUCTURE.md"
triggers:
  - "CI"
  - "GitHub Actions"
  - "workflow"
  - "合併前"
  - "runner"
created: 2026-10-08
---

# CI 先停用，合併前由本機驗證

## Context

範本附有 `.github/workflows/ci.yml`（建置＋測試），但公司 GitHub Enterprise 停用了 hosted runner，源專案的 CI 執行全部在數秒內失敗、從未成功。2026-10-08 源專案使用者決定：在本機開發，CI 先停用；新專案在同一個 GitHub Enterprise 下，沿用此決定。

## Decision/Rule

- 新專案的 repo 建立後，於 GitHub 停用 CI workflow（`gh workflow disable CI`），`ci.yml` 保留；有可用 runner（開放 hosted runner 或自架 self-hosted runner）後以 `gh workflow enable CI` 恢復。
- 合併前由作者在本機跑 `dotnet build TcmClinic.Api.slnx` 與單元、整合、功能、驗收四層測試，全綠才合併；新增資料夾或檔案時另以乾淨 clone 確認建置（避免檔案被 `.gitignore` 排除而未進版控）。
- 不以 GitHub 上的 CI 狀態作為合併依據；PR 頁面不會出現 CI 結果。

## Constraint

- 依賴 CI 的事項維持延後：覆蓋率基準自動比對（DEF-018，暫不做）。
- 恢復 CI 時同步更新 AGENTS.md「建置與 CI」列、`docs/INFRASTRUCTURE.md` §4 與 `ci.yml` 開頭註記。
- 依據：AGENTS.md「已裁決的衝突」建置與 CI 列。
