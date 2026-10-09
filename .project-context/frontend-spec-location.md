---
id: PRJ-2026-0012
type: convention
status: active
confidence: 1.0
scope:
  - "specs/**"
  - "oldSpec/**"
triggers:
  - "前台規格"
  - "specs/前台"
  - "oldSpec"
created: 2026-10-09
---

# 前台規格的位置

## Context

後台規格依選單分層放 `specs/<業務>/<作業>/<功能>/`，但公開網站沒有選單。2026-10-09 使用者確認前台規格的放法。

## Decision/Rule

- 前台規格放 `specs/前台/<頁面或功能>/spec.md`（如 `specs/前台/網上預約/`）。
- 後台功能照選單分層，前後端寫在同一份規格（API 契約與畫面行為一起描述）。
- `oldSpec/` 是舊站（syttcm.com）的逆向分析，只是撰寫新規格的參考資料，不是規格；新功能仍走 SDD 流程。

## Constraint

- 依據：`docs/FRONTEND.md` §13、`specs/README.md` 資料夾規則。
