# 架構決策紀錄（ADR）

重要決策照 UDS [adr-standards](../../.standards/adr-standards.ai.yaml) 一個決策一檔 `ADR-NNN-短描述.md`：

- **位置**：全部放在本資料夾 `docs/adr/`，不分選單或跨選單（2026-10-09 起，對齊 UDS；之前放在對應規格的資料夾與 `specs/其他/架構決策/`）。
- **編號**：全專案共用流水號；ADR-001～011 為範本內建（沿用源專案），新決策從 ADR-012 起接續。本表為唯一索引。
- **不可修改**：決策改變時另開新 ADR 取代舊的，舊 ADR 狀態改為「已取代」並註明取代者。
- **與規格的關係**：規格的 Technical Design 列出並連結相關 ADR，ADR 的「出處」連回規格。範本內建規格的「設計決策 R1～Rn」維持在規格內，不搬成 ADR。

| 編號 | 決策 | 狀態 | 日期 |
|------|------|------|------|
| ADR-001 | [選單導向授權：選單可見性＝該頁所有操作的權限](./ADR-001-menu-based-authorization.md) | Accepted | 2026-08-31 |
| ADR-002 | [跨表關聯一律以 ID 外鍵參照，不用業務代碼](./ADR-002-relations-by-id-foreign-key.md) | Accepted | 2026-09-30 |
| ADR-003 | [所有資料表一律軟刪除，並具備五個標準欄位](./ADR-003-soft-delete-and-standard-columns.md) | Accepted | 2026-09-30 |
| ADR-004 | [多人共用維護的資料以 RowVersion 樂觀併發，雙道防線回 409](./ADR-004-optimistic-concurrency-rowversion.md) | Accepted | 2026-09-21 |
| ADR-005 | [主鍵用 Vogen 強型別 ID，由程式端以 Max+1 產生](./ADR-005-vogen-ids-client-generated.md) | Superseded（ADR-010） | 未記錄（專案初期） |
| ADR-006 | [使用者操作紀錄 SysUserLog：獨立交易、盡力寫入、成功失敗都記](./ADR-006-user-operation-log.md) | Accepted | 2026-09-29 |
| ADR-007 | [分頁清單預設每頁筆數固定為 10](./ADR-007-default-page-size-fixed.md) | Accepted | 2026-10-06 |
| ADR-008 | [生產日誌改 JSON，並依等級分檔保留](./ADR-008-production-json-logs-by-level.md) | Accepted | 2026-10-08 |
| ADR-009 | [密碼雜湊改 PBKDF2，既有雜湊於登入成功時自動轉換](./ADR-009-password-hashing-pbkdf2.md) | Accepted | 2026-10-08 |
| ADR-010 | [Vogen 強型別主鍵改由資料庫 IDENTITY 配號](./ADR-010-identity-primary-keys.md) | Accepted | 2026-10-08 |
| ADR-011 | [登入改用 JWT Bearer（短效 access token＋可撤銷的 refresh token）](./ADR-011-jwt-bearer-authentication.md) | Accepted | 2026-10-08 |

> 全專案的日常約定（每頁筆數、表名前綴、非預期錯誤訊息、開發流程、規範裁決、CI）記在 `.project-context/`（PRJ-2026-0001、0003、0004、0007～0009）。
