---
id: PRJ-2026-0003
type: convention
status: active
confidence: 1.0
scope:
  - "backend/src/TcmClinic.Api.Infrastructure/Data/Config/**"
  - "backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/**"
triggers:
  - "資料表"
  - "表名"
  - "ToTable"
  - "Sys 前綴"
created: 2026-10-06
---

# 系統表加 `Sys` 前綴，業務表不加

## Context

所有表都加 `Sys` 前綴時，無法從表名分辨平台設施與業務資料。源專案因此確立區分原則，並寫入 `docs/DATABASE-SCHEMA.md` §8。

## Decision/Rule

- **系統表**（身分、選單、角色、稽核、日誌、系統設定等平台設施）加 `Sys` 前綴，如 `SysUser`、`SysUserLog`、`SysAuditLog`。
- **業務表**（某業務領域的概念）不加前綴，如 `Order`、`Customer`、`Invoice`。
- 判準：「這是不是某個業務領域的概念」。
- 表名一律 PascalCase 單數（見 AGENTS.md「已裁決的衝突」）。

## Constraint

- 新表命名前先依判準歸類，並在規格的資料模型註明歸類理由。
- 依據：`docs/DATABASE-SCHEMA.md` §8。
