---
id: PRJ-2026-0004
type: constraint
status: active
confidence: 1.0
scope:
  - "backend/src/TcmClinic.Api.UseCases/**"
triggers:
  - "非預期錯誤"
  - "例外"
  - "CriticalError"
  - "失敗訊息"
  - "操作紀錄"
created: 2026-10-06
---

# 非預期錯誤回固定訊息，不外洩例外內容

## Context

常見缺陷：維護功能在 catch 後把例外原文顯示給使用者並寫入操作紀錄，內含表名、約束名等內部資訊。

## Decision/Rule

- 用例攔截非預期例外（**排除 `OperationCanceledException`**），流程：
  1. 以 `logger.LogError(ex, …)` 記錄完整例外到應用程式日誌。
  2. 寫一筆失敗的使用者操作紀錄，內容只用固定訊息（如「帳號維護 [xxxx]新增失敗」）。
  3. 回傳 `Result.CriticalError(固定訊息)`，由 ResultExtensions 轉成 500 ProblemDetails。
- 業務規則失敗（如「帳號已存在」）可在回應與操作紀錄附上原因，但不得含例外原文。
- 單元測試至少涵蓋「非預期錯誤回固定訊息」與「取消例外不被攔截」兩案。

## Constraint

- 回應、操作紀錄禁止出現例外訊息、堆疊、SQL 或表名。
- 依據：[SPEC-013 個人資料維護](../specs/管理業務/系統管理作業/個人資料維護/spec.md) 的 `CriticalError → 500` 對映；`docs/SECURE-CODING.md` 的 log level 規則（`Error` 附例外物件）；ADR-006。
