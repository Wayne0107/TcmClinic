---
id: PRJ-2026-0007
type: convention
status: active
confidence: 1.0
scope:
  - "backend/src/TcmClinic.Api.UseCases/**/List*.cs"
  - "backend/src/TcmClinic.Api.UseCases/**/Get*.cs"
  - "backend/src/TcmClinic.Api.UseCases/Paging/**"
  - "backend/src/TcmClinic.Api.Web/Endpoints/**/*Endpoint.cs"
triggers:
  - "分頁"
  - "清單"
  - "pageSize"
  - "每頁筆數"
created: 2026-10-06
---

# 清單預設每頁筆數固定 10

## Context

清單預設每頁筆數若取自可調整的參數，每個清單都要多一次讀取，行為也隨設定變動、難以測試。2026-10-06 源專案使用者決定改為固定值，決策見 [ADR-007](../specs/其他/架構決策/ADR-007-default-page-size-fixed.md)。

## Decision/Rule

- 請求的 `pageSize` 宣告為可空（`int?`），不在 request DTO 設初始值（FastEndpoints 的 DTO 初始值會在進入 handler 前生效）。
- 未指定、0 或負數時為 10；指定時依指定值，上限 100。
- handler 一律呼叫 `PageSizeResolver.Resolve`（`backend/src/TcmClinic.Api.UseCases/Paging/`）；回應與 `Link` 標頭用實際採用的筆數。
- 新的分頁清單要加進功能測試 `ListPageSizeTests.Lists`（清單路徑與其選單 value）。

## Constraint

- 禁止在清單 handler 或 DTO 寫死預設筆數，也禁止改由設定檔或資料庫參數決定每頁筆數。
- 依據：`docs/API-CONVENTIONS.md` §4、[ADR-007](../specs/其他/架構決策/ADR-007-default-page-size-fixed.md)。
