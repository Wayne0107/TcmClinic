# ADR-005 主鍵用 Vogen 強型別 ID，由程式端以 Max+1 產生

| 項目 | 內容 |
|------|------|
| 狀態 | Superseded（2026-10-08 由 [ADR-010](ADR-010-identity-primary-keys.md) 取代：改由資料庫 IDENTITY 配號） |
| 日期 | 確切日期未記錄（專案初期即採用，憲章原則 IV「類型安全」；2026-10-06 依憲章 2.1.0 補記為 ADR） |
| 決策者 | Wayne（維護者） |
| 出處 | `docs/DATABASE-SCHEMA.md` §4；`docs/CONSTITUTION.md` 原則 IV |

> 📦 本 ADR 隨範本沿用自源專案（CHB），決策本身仍適用於範本。背景、影響與相關連結中提到的舊系統、分行、企業、開狀申請、系統參數等屬源專案業務，相關規格、資料表與 migration 範本已移除；以骨架程式碼與測試為準。

## 背景（Context）

憲章原則 IV 規定以強型別表達業務概念、禁止以裸原始型別作為領域識別，避免把「角色 ID」誤傳成「使用者 ID」這類錯誤。專案以 Vogen 產生值物件型別作為主鍵。但 EF Core 在追蹤新實體時會對 ID 呼叫 `GetHashCode()`，未初始化的 Vogen 結構會擲例外，與 SQL Server `IDENTITY`「存檔後才有值」的模式衝突。

## 考慮過的選項

1. **Vogen 強型別 ID＋程式端 `ValueGenerator` 以 `Max(Id)+1` 指派，資料表不用 `IDENTITY`**。
2. **裸 `int` 主鍵＋`IDENTITY`**：EF 預設做法，但違反憲章原則 IV。
3. **Vogen ID＋`IDENTITY`，以哨兵值暫代**：需要處理追蹤時的雜湊問題，且新增前拿不到真正的 ID。
4. **GUID 主鍵**：不需資料庫產生，但叢集索引容易分散、可讀性低，且與既有資料表的整數主鍵不一致。

## 決策結果

採用 **選項 1**：

- 每個聚合與子實體各有 Vogen `int` ID（如 `BranchId`、`EnterpriseId`），驗證規則允許 `>= 0`，以 0 作為新建時的哨兵。
- EF 設定 `HasValueGenerator<…IdValueGenerator>()`、`HasSentinel(…From(0))`，並關閉 SQL Server 的值產生策略；遷移檔不得產生 `IDENTITY`。
- LINQ 內直接比較 Vogen 型別，不存取 `.Value`。
- **例外**：append-only 稽核、日誌表可用 `bigint IDENTITY`（`docs/DATABASE-SCHEMA.md` §6），須在規格註明。

**理由**：編譯期就擋下 ID 誤用；新增前即有有效 ID，EF 追蹤不出錯；整數主鍵簡單、索引效率好。舊系統以業務代碼（統編、機構代碼字串）當主鍵，新系統改由 ID 關聯（ADR-002），業務代碼只當唯一業務鍵。

## 後果

- **好**：方法簽章與外鍵都用強型別，傳錯 ID 無法編譯；外鍵型別一致（ADR-002）。
- **壞**：`Max+1` 在極高併發新增時可能撞號，由主鍵約束擋下、該次存檔失敗（目前沒有通用重試機制；後台管理資料的新增頻率低，可接受）；每個實體都要寫一個 ValueGenerator 與轉換設定；開發者要記得 LINQ 不能用 `.Value`。

## 連結

- `docs/DATABASE-SCHEMA.md` §4、§6
- 程式：`backend/src/TcmClinic.Api.Infrastructure/Data/ValueGenerators/`
