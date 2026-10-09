---
id: PRJ-2026-0008
type: convention
status: active
confidence: 1.0
scope:
  - "docs/**"
  - "AGENTS.md"
  - "specs/README.md"
  - "backend/**"
triggers:
  - "UDS"
  - "已裁決"
  - "規範衝突"
  - "照專案"
  - "改採 UDS"
  - "ProblemDetails"
  - "IDENTITY"
  - "日誌格式"
  - "版本號"
created: 2026-10-08
---

# 專案規範與 UDS 逐題裁決（74 題）

## Context

源專案的規範（憲章、`docs/`、AGENTS.md）與 UDS 標準（`.standards/`）逐條比對後，做法不同的共 74 題：44 題原本已裁決，30 題從未記錄。使用者於 2026-10-08 以勾選表逐題決定照專案、改採 UDS 或待討論。對照與勾選結果的快照見 `docs/STANDARDS-COMPARISON.html`、`docs/STANDARDS-DECISIONS.html`。範本沿用這份裁決結果。

## Decision/Rule

- **照專案（61 題）**：維持專案做法，記入 AGENTS.md「已裁決的衝突」表。
- **改採 UDS（12 題）**，`docs/` 已改為新規則，範本骨架已落實：
  - 所有錯誤回應一律 ProblemDetails（含 404、401／403、驗證器 400、未處理例外 500）。
  - Vogen 主鍵由 SQL Server IDENTITY 配號；金額 `decimal(19,4)`。
  - 秘密掃描提前到每次 commit（pre-commit hook）。
  - 生產日誌 JSON（snake_case 必要欄位）、依等級保留 7／30／90／365 天；請求日誌加記 source_ip。
  - 變異測試定期或發版前執行，不放 commit hook、不在每個 PR 跑。
  - 密碼雜湊用 PBKDF2（撤銷加鹽 SHA-256 例外）。
  - `docs/` 規範文件標示版本與最後更新日期（`docs/COMMENTS.md` §8）。
  - 版本號採 SemVer，提供 `GET /version`（version、sha、buildTime）。
  - 對外識別碼用 UUIDv7 `PublicId`，主鍵維持 int IDENTITY（SPEC-064）。

## 後續變更（2026-10-08 同日）

- 分頁：原則上一律 offset，有效能需求時再於該清單的規格評估 keyset（取代原本的「大量或即時資料用 keyset」）。
- 速率限制：登入端點依來源 IP 限速（每分鐘 20 次，SPEC-004 FR-006b）。
- 覆蓋率基準自動比對暫不做（DEF-018）；CI 先停用，見 [PRJ-2026-0009](ci-disabled-local-verification.md)。
- 變異測試：Stryker.NET 5.0.0 在本專案無法產生有效分數，工具修正前不以分數為依據（DEF-021）。

## Constraint

- 遇到專案與 UDS 不同的議題，先查 AGENTS.md 已裁決表與本紀錄；已裁決的不再重新提問。
- 規則改變但程式尚未跟上時，在 `docs/` 註記「現況尚未符合，見 DEF-0xx」並登記到 specs/README.md 延後事項；實作完成時同步移除註記並更新 DEF 狀態。
- 依據：AGENTS.md「已裁決的衝突」與其 2026-10-08 註記；specs/README.md 延後事項。
