# 註解與文件規範（Comments & Documentation）

> 版本 1.2.0｜最後更新 2026-10-09

> 本檔為憲章原則 V（系統一致性）與 VII（繁體中文）的 **How**。
> 註解的**語言**一律繁體中文；註解的**結構**依下列分層。

## 0. 語言範圍

必須用繁體中文：規格/計畫/任務、清單與分析報告、使用者文件、git commit 訊息、API 端點 metadata（`Summary`/`Description`/`Responses`）、程式碼註解（`//`、`/// <summary>`、`/* */`）。

維持英文：原始碼識別符（變數、類別、方法名，遵循 Microsoft 命名慣例）、範本佔位符（如 `[FEATURE NAME]`）、需要英文的第三方屬性值（route template、content type）。

前端（TypeScript、Vue）同樣適用：註解用繁體中文、識別字用英文。下方各節的 C# 寫法在前端的對應方式：文件式用 TSDoc（`/** … */`），測試意圖寫在 `it(...)` 的繁中說明與前一行註解（`docs/FRONTEND.md` §4.5、§8.3）。

## 1. 文件式（Documentation-style）

回答「這是什麼／怎麼用」。用於公開 API 表面、共用介面、library/工具類（其使用者看不到實作）。因驅動 IDE IntelliSense，正確性是第一要務。

- 必須用標準 C# XML doc：`<summary>`（用途）、`<param>`（每個參數意義）、`<returns>`（回傳意義，含邊界結果如「無折扣時回傳 0」）、`<exception>`（每個可能擲出的例外型別 + 觸發條件）。

```csharp
/// <summary>
/// 根據訂單編號計算該筆交易的最終折扣金額。
/// </summary>
/// <param name="orderId">系統生成的唯一訂單 GUID</param>
/// <param name="couponCode">選填。使用者輸入的折價券代碼</param>
/// <returns>回傳計算後的折扣總額；若無適用折扣則回傳 0</returns>
/// <exception cref="OrderNotFoundException">
/// 當 <paramref name="orderId"/> 找不到對應訂單時拋出。
/// </exception>
public decimal CalculateDiscount(Guid orderId, string? couponCode)
```

## 2. 規格式（Specification-style）

回答「為何這樣寫／有何業務限制」。用於核心領域邏輯、複雜演算法、非顯而易見的轉折處。目的：保存業務決策脈絡，避免後人誤刪關鍵邏輯。

- 必須用區塊註解（`/* ... */`），依序記錄：
  1. 業務需求 + 變更日期 + **可追溯單號**（如 `[商業規範 2026-04 Ticket #PROMO-502]`）。
  2. 邊界條件與邏輯限制。
  3. **為何**這樣設計——不是逐行複述程式碼在做什麼。

```csharp
/*
 * [商業規範 2026-04 Ticket #PROMO-502]
 * 1. 跨境交易必須優先扣除手續費，再計算紅利點數。
 * 2. 若餘額不足，需標記訂單狀態為 Pending，不可直接拋出異常。
 * 3. 限制：單筆交易金額上限為 NT$ 1,000,000 (金流業者規範)。
 */
```

## 3. 維運式（Operations-style）

回答「壞掉時看哪裡／要盯什麼」。用於基礎設施層、外部 API 整合、背景工作/排程、效能敏感區段。目的：協助 on-call 快速定位生產事故。

- 必須用 `[維運提示]` 標記註解，含：依賴服務（服務 + endpoint）、預期延遲/效能風險與其失敗閾值、可搜尋的監控 **log 關鍵字**（括號 tag，如 `[PUSH_GATEWAY_LIMIT]`）+ 觸發時的應變措施。
- 錯誤處理必須記錄 correlation id。

```csharp
// [維運提示]
// 依賴服務：CRM 系統 API (https://api.crm.internal)
// 預期延遲：~500ms；若超過 2s 可能導致連線池溢滿。
// 監控關鍵：[PUSH_GATEWAY_LIMIT] 出現 HTTP 429 時觸發；
//           應變：檢查 Rate Limiting 設定並啟用指數退避重試。
```

## 4. 測試意圖式（Test-intent）

回答「驗證什麼情境、預期結果為何」。用於**每個**測試。詳見 `docs/TESTING.md` §5。

```csharp
// 情境：刪除仍有子項的父選單 → 應阻擋（FR-004），不可連動刪除整棵子樹。
[Fact(DisplayName = "刪除仍有子項的父選單時應阻擋，不連動刪除子樹（FR-004）")]
public async Task Delete_blocks_when_item_has_children() { /* ... */ }
```

## 5. 執行矩陣

| 層 | 風格 | 嚴格度 |
|----|------|--------|
| 公開 API / Utils（Web 端點、共用、Core 公開成員） | 文件式 | MUST——須 XML doc |
| 服務 / 業務邏輯（UseCases、Domain） | 規格式 | 非顯而易見的決策 MUST；瑣碎邏輯 MAY 省略 |
| 基礎設施 / 工作（外部整合、效能敏感） | 維運式 | MUST 標記風險點（依賴、延遲、監控） |
| 測試（單元/整合/功能/屬性） | 測試意圖 | MUST——每測試註明情境與預期，且 `[Fact]`/`[Theory]` 須帶繁中 `DisplayName` |

## 6. 反模式（禁止）

- **過期註解**：程式碼變更時，其註解必須於同次變更更新；與程式碼矛盾的註解 = 缺陷。
- **冗餘註解**：僅複述自我說明識別符的註解（如在 `UpdateUserStatusToActive()` 上寫「更新使用者狀態為啟動」）。名稱已說明就省略。

## 7. 審查閘門

- Code Review 必須驗證註解新鮮度：看到變更的方法其註解已與程式碼不符，必須擋下直到修正。
- 必須退回缺測試意圖註解或 `DisplayName` 的新增/變更測試。

## 8. 規範文件的版本標頭

- `docs/` 下的規範文件（不含憲章，憲章依其 Governance 的版本策略）在標題下方標示版本與最後更新日期：`> 版本 X.Y.Z｜最後更新 YYYY-MM-DD`（2026-10-08 起，對齊 UDS）。
- 規則實質變更（新增、刪除、改變要求）升 MINOR；措辭澄清、補例子、修正錯字升 PATCH；文件重新架構升 MAJOR。
- 修改文件時同次更新版本與日期；2026-10-08 以前的變更不回溯，歷史以 git 為準（`docs/` 自 2026-10-09 起進版控）。
