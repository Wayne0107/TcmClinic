# ADR-010 Vogen 強型別主鍵改由資料庫 IDENTITY 配號

| 項目 | 內容 |
|------|------|
| 狀態 | Accepted |
| 日期 | 2026-10-08 |
| 決策者 | Wayne（維護者；授權 AI 直接實作） |
| 出處 | 開發規範裁決 2026-10-08（PRJ-2026-0008）第 2 題；specs/README.md 延後事項 DEF-012 |
| 取代 | [ADR-005](ADR-005-vogen-ids-client-generated.md)（主鍵由程式端以 Max+1 產生） |

> 📦 本 ADR 隨範本沿用自源專案（CHB），決策本身仍適用於範本。背景、影響與相關連結中提到的舊系統、分行、企業、開狀申請、系統參數等屬源專案業務，相關規格、資料表與 migration 範本已移除；以骨架程式碼與測試為準。

## 背景（Context）

ADR-005 讓 Vogen 強型別主鍵由程式端計算 `Max(Id)+1`（每種 ID 一個 `ValueGenerator`，共用 `MaxIdCalculator`），理由是 EF Core 追蹤新實體時會對未初始化的 Vogen 結構呼叫 `GetHashCode()` 而擲例外，與 SQL Server `IDENTITY`「存檔後才有值」的模式衝突。

逐條比對 UDS 標準後（UDS 建議內部 ID 用自動遞增），重新檢視這個做法，發現兩個問題：

- **同時新增會撞號**：兩個請求在對方存檔前各自計算 `Max+1`，得到同一個值，後存檔者違反主鍵唯一性而回 500。2026-10-08 以 RefuseReason 寫測試重現（重複值 6）。
- **每新增一筆就載入整張表**：`MaxIdCalculator` 以 `AsEnumerable()` 把整表讀進記憶體計算最大值，持續成長的表（如開狀申請）新增會越來越慢。

2026-10-08 以 RefuseReason 試驗改用 IDENTITY，確認可行：

- 實體建構時已指派哨兵 `Id = XxxId.From(0)`，EF 不會碰到未初始化的結構；真正的阻礙是 EF 在存檔前指派**負數暫時值**，被 Vogen 的 `>= 0` 驗證擋下。放寬驗證即可。
- SQL Server 不能以 `ALTER COLUMN` 為既有欄位加上 `IDENTITY`，EF 自動產生的 migration 會失敗，必須重建資料表。

## 考慮過的選項

1. **全部 17 張表改 IDENTITY，以通用的重建程式產生 migration**：migration 執行時讀取資料庫目錄，自動重建欄位、主鍵、索引、CHECK、中文說明與外鍵，不必逐表手寫。
2. **逐表手寫重建 migration**：每表約 70 行，17 張表容易抄錯欄位、說明或外鍵。
3. **維持 Max+1，只修 `MaxIdCalculator`**（SQL 端算 MAX、撞號時重試）：改動最小，但仍是應用程式自己配號，撞號只是機率變低，不是消除。

## 決策結果

採用 **選項 1**：

- 17 個 Vogen 主鍵（Branch、BranchEmail、BranchSubordinate、DispatchRecord、Enterprise、EnterpriseBranch、LcApplication、LcApplicationAttachment、LcApplicationContent、MessageInbox、RefuseReason、SysTask、SysRole、SysRoleTask、SysParameter、SysUser、SysUserRole）改為 `IDENTITY(1,1)`。
- EF 設定：`.ValueGeneratedOnAdd().UseIdentityColumn().HasSentinel(XxxId.From(0))`；刪除 18 個 `ValueGenerator` 檔與 `MaxIdCalculator`。
- Vogen ID 的 `Validate` 不再限制正負（EF 存檔前的暫時值為負數）。
- migration `IdentityPrimaryKeys` 以 `IdentityRebuildSql` 逐表重建：保存並移除指進、指出的外鍵 → 舊表與其主鍵、CHECK 約束改名 → 依舊表欄位定義建新表（`Id` 加 `IDENTITY`） → `IDENTITY_INSERT` 保留原 Id 搬資料 → 刪舊表 → 重建索引、CHECK、中文說明 → 重建外鍵。rowversion 欄位無法指定值，搬資料時略過，重新產生。Down 以同一程式重建為非 IDENTITY。
- 整個 migration 在單一交易內執行，任一表失敗即整體回復。
- 稽核攔截器（SPEC-006）原本依賴「主鍵於 Add 時已指派」，改為兩段式：存檔前擷取操作類型與改前快照，必要時自行開交易；存檔後以真實主鍵與外鍵補正改後快照、寫入稽核紀錄再提交。資料與稽核紀錄仍在同一交易內同成同敗。
- **範圍只限內部主鍵**。UDS 同條規則的另一半「對外 ID 用 UUIDv7」另案處理（延後事項 DEF-020）：主鍵維持 int IDENTITY，對 API 公開的實體另加 UUIDv7 欄位。

**理由**：

- 由資料庫配號，同時新增不會撞號，新增也不必讀整張表。
- 重建程式以資料庫目錄為準，不會漏抄欄位或外鍵；以 EF 模型驗證外鍵、索引仍完整的測試把關。
- 系統尚未上線，採停機部署（`docs/DATABASE-SCHEMA.md` §9），重建資料表的停機時間可接受。

## 後果

- **好**：
  - 消除撞號造成的 500。
  - 新增實體的效能不再隨資料量下降。
  - 少了 18 個產生器類別與相關規則。
- **壞**：
  - Vogen ID 不再擋負數；負數 ID 只會查無資料（回 404），不會造成錯誤寫入。
  - 重建期間各表短暫不存在，必須停機執行。
  - 有 rowversion 的表（Branch、Enterprise、LcApplication、SysRole、SysParameter）在遷移後版本值全部更新；遷移前取得的舊版本值送出修改時會得到 409，重新載入即可。
  - 從舊系統搬資料時，要保留原 Id 須以 `SET IDENTITY_INSERT ... ON` 寫入。
  - 新增受稽核實體的存檔多一次寫入（稽核紀錄改在主資料存檔後寫入）。
  - 既有規格資料模型中「Max+1 `ValueGenerator`（非 IDENTITY）」的描述保留為歷史紀錄，以本 ADR 為準。

## 連結

- 規範：[docs/DATABASE-SCHEMA.md](../../../docs/DATABASE-SCHEMA.md) §4.1、§6；[docs/CODING-STANDARDS.md](../../../docs/CODING-STANDARDS.md) Vogen 規則
- 延後事項：[specs/README.md](../../README.md) DEF-012
- 程式：`backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/IdentityRebuildSql.cs`、migration `IdentityPrimaryKeys`、`backend/src/TcmClinic.Api.Infrastructure/Data/Config/*Config.cs`
- 測試：`backend/tests/TcmClinic.Api.IntegrationTests/Data/IdentityPrimaryKeyTests.cs`
