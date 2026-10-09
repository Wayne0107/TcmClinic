# 資料庫 Schema 規範（Database Schema）

> 版本 1.3.0｜最後更新 2026-10-08

> 本檔為憲章原則 IV（類型安全）與 III（架構一致性）在資料層的 **How**。
> 資料表命名（系統表 vs 業務表、`Sys` 前綴）見 §8。

## 1. 標準欄位

每個持久化實體（資料表）必須定義以下標準欄位：

| 欄位 | 說明 |
|------|------|
| `Id` | 主鍵，必須用 Vogen 強類型 ID（見 §4） |
| `CreateDate` | 建立時間戳，INSERT 時設定一次，之後不變 |
| `CreateUser` | 建立者帳號，INSERT 時設定一次 |
| `ModifyDate` | 最近更新時間戳，每次 UPDATE 刷新 |
| `ModifyUser` | 最近更新者帳號，每次 UPDATE 刷新 |
| `Removed` | 軟刪除旗標（SQL `bit`，NOT NULL，預設 `0`）；`1` 為邏輯刪除 |

**審計與軟刪除五欄（`CreateDate`／`CreateUser`／`ModifyDate`／`ModifyUser`／`Removed`）所有資料表一律具備、格式統一**，含系統表與 append-only 稽核／日誌表（§6 不得省略），不得另用 `OperatedBy`／`OperatedAt`、`AccessedBy`／`AccessedAt` 等自訂名稱：

| 欄位 | SQL 型別 | 可空 | 預設 |
|------|----------|------|------|
| `CreateDate` | `datetime` | NOT NULL | — |
| `CreateUser` | `nvarchar(50)` | NOT NULL | — |
| `ModifyDate` | `datetime` | NOT NULL | — |
| `ModifyUser` | `nvarchar(50)` | NOT NULL | — |
| `Removed` | `bit` | NOT NULL | `0` |

- `CreateDate`/`ModifyDate` 儲存應用程式的**當地區域時間**（非 UTC）。
- INSERT 時 `ModifyDate`/`ModifyUser` 與 `CreateDate`/`CreateUser` 同值；之後每次 UPDATE（含軟刪除）只刷新 `ModifyDate`/`ModifyUser`。
- `CreateUser`/`ModifyUser` 存操作者的**登入帳號**；無登入者時存固定標識：背景作業／種子資料為 `system`，未驗證請求為 `anonymous`。
- **這兩欄是 §5「關聯一律用 `Id`」的唯一例外**：它們是操作者身分的紀錄，不是跨表關聯，不建 FK、不作為 join 條件。

## 2. 審計欄位與軟刪除

審計欄位與 `Removed` 必須：

- 宣告於共用可審計基底類別（如 `AuditableEntityBase`），不逐實體重複。
- 由 EF Core `SaveChangesInterceptor` 自動填入，從 current-user 抽象（如 `ICurrentUser`）讀取操作者。應用與 use-case 程式碼不得手動設定這些欄位。

軟刪除（`Removed`）：

- 刪除預設為邏輯刪除（設 `Removed = 1`），不發實體 `DELETE`。硬刪除禁止，除非規格明確說明（如法遵資料抹除）。
- 全域查詢過濾器必須排除 `Removed = 1`；要讀已刪除列必須明確 `IgnoreQueryFilters`。
- 需忽略已刪除列的唯一性約束，必須用 `Removed = 0` 的 filtered index（讓已軟刪值可重用）。

## 3. 欄位文件

- 每個欄位必須帶說明，透過 EF Core `.HasComment(...)` 寫入資料庫（SQL Server `MS_Description`），讓 schema 在工具（SSMS 等）中自我說明。含標準審計欄位、`Id`、`Removed`。
- 每個資料表也必須帶實體層級說明。
- 說明文字使用繁體中文。
- 標準審計/`Id`/`Removed` 的說明宜於共用設定（`AuditableEntityBase` mapping）套用一次。
- 新增或變更欄位/表卻缺正確說明 = 缺陷，須於審查中擋下。

## 4. Vogen 強類型 ID 的 EF Core 整合

Vogen ID 需要特定 EF Core 設定，違反會造成執行期例外。

### 4.1 主鍵由資料庫 IDENTITY 配號

有 Vogen ID 的實體以 SQL Server `IDENTITY(1,1)` 配號（2026-10-08 裁決，對齊 UDS「內部 ID 用自動遞增」）。客戶端 `Max(Id) + 1` 會在兩人同時新增時撞號（違反主鍵），且每次新增都載入整張表計算最大值，故不再使用。

```csharp
builder.Property(e => e.Id)
    .HasConversion(id => id.Value, value => TId.From(value))
    .ValueGeneratedOnAdd()
    .UseIdentityColumn()
    .HasSentinel(TId.From(0));   // 新實體建構時 Id = TId.From(0)，代表尚未配號
```

- Vogen ID 的 `Validate` **不得**限制正負：EF 存檔前會指派負數暫時值，存檔後才回填資料庫配發的號碼。
- 既有欄位不能以 `ALTER COLUMN` 加上或移除 `IDENTITY`，EF 自動產生的 migration 會失敗；須在 migration 內重建資料表（源專案以 `IdentityRebuildSql` 輔助類別於執行時讀取資料庫目錄處理，需要時可移植）：拆除指進、指出的外鍵 → 依舊表定義建含 `IDENTITY` 的新表 → `SET IDENTITY_INSERT ON` 保留原 Id 搬資料（rowversion 略過、重新產生）→ 刪舊表 → 重建索引、CHECK 約束、說明與外鍵。
- 從其他系統搬資料需保留原 Id 時，同樣以 `IDENTITY_INSERT` 寫入。
- 所有 Vogen 主鍵一律 IDENTITY（[ADR-010](../specs/其他/架構決策/ADR-010-identity-primary-keys.md)）；新增實體照上面的設定，`IdentityPrimaryKeyTests` 檢查 Id 為 IDENTITY 且外鍵、索引、說明與模型一致。

### 4.1a 對外識別碼 PublicId（UUIDv7）

識別碼會出現在 API 的實體，除了 IDENTITY 主鍵，另加對外識別碼 `PublicId`（SPEC-064，2026-10-08，對齊 UDS「分散式或對外 ID 用 UUIDv7」）。主鍵、外鍵與關聯照舊用 int，`PublicId` 只給 API 使用。

- 型別：每種實體一個 Vogen `Guid` 型別（如 `UserPublicId`），`New()` 以 `Guid.CreateVersion7()` 產生、驗證拒絕 `Guid.Empty`；實體以 `{ get; private set; }` 宣告，在建構式產生，建立後不可變更。
- 欄位：`uniqueidentifier NOT NULL`，唯一索引 `IX_<表>_PublicId`，說明「對外識別碼（UUIDv7，API 一律使用此值，不公開整數主鍵；SPEC-064）」；EF 設定 `.ValueGeneratedNever()`（值由應用程式產生，不用資料庫預設值）。
- 為既有資料表新增時，migration 以預設值 `NEWID()` 加入欄位補既有資料，再移除預設值並建唯一索引；SQL Server 沒有內建 UUIDv7，補值為 v4 可接受。
- 骨架中有 `PublicId` 的表：`SysUser`、`SysRole`、`SysTask`、`SysUserSession`（`PublicIdSchemaTests` 檢查）；新增會出現在 API 的實體一併加入。只以業務代碼對外識別的表與日誌表不加。

### 4.2 LINQ 表達式限制

EF Core LINQ 表達式（Where/OrderBy/Select）內**不得**存取 Vogen ID 的 `.Value`：

```csharp
// ❌ 錯誤——EF Core 無法翻譯 .Value
Query.Where(p => p.CategoryId.Value == categoryId.Value);

// ✅ 正確——直接比較強類型 ID
var catId = CategoryId.From(rawIntValue);
Query.Where(p => p.CategoryId == catId);
```

### 4.3 Vogen EF 轉換

所有 Vogen ID 屬性必須在其 `IEntityTypeConfiguration` 呼叫 `.HasVogenConversion()`，註冊正確的 `ValueConverter`。

## 5. 外鍵

- 實體間每個關係必須以明確外鍵建模，並在 `IEntityTypeConfiguration` 設定（`HasOne`/`HasMany`、`WithMany`/`WithOne`、`HasForeignKey`）。
- **跨表關聯一律參照被參照表的 `Id` 主鍵**，不得以業務欄位（如機構代碼、部門代碼、單據編號、帳號等）作為關聯鍵或外鍵目標（不用 `HasPrincipalKey` 指向業務欄位／alternate key）。業務欄位可能變更、重用或跨來源不一致，只作為顯示與查詢條件，需要時由關聯表 join 取得。
- 外鍵欄位命名為 `<被參照實體>Id`（如 `RoleId`、`UserId`；同一表參照同一實體多次時加角色前綴，如 `ApproverUserId`），型別必須用被參照聚合的 Vogen 強類型 ID，不用裸原始型別。
- 外部輸入（API 請求、匯入檔）若以業務代碼識別關聯對象，必須在應用層先解析為 `Id` 再寫入；不得把業務代碼直接存成關聯欄位。
- **適用所有資料表**：業務表、系統表、append-only 稽核／日誌表一體適用；唯一例外是 §1 的 `CreateUser`／`ModifyUser`（存帳號）。其他需要指向使用者的欄位（如負責人、審核人）仍須存 `UserId`。
- 需保留「當下的值」（如送出當時的部門名稱）時，可在 `Id` 之外另存快照欄位；快照只供顯示，不得作為關聯或 join 條件。
- 被參照對象若本系統尚無對應資料表（如外部機構代碼），須先建主檔再以其 `Id` 參照，不得直接存代碼作為關聯。
- 刪除行為（`OnDelete`）必須明確宣告（如 `Restrict`、`Cascade`），不依賴 EF Core 預設。
- 參照完整性必須在資料庫層以 FK 約束強制，不只在應用程式碼。

## 6. 例外——append-only 審計/技術表

append-only、不可變的表（如資料審計日誌）欄位集與一般表相同（見 §1），僅以下幾點可不同：

- 主鍵用 `bigint IDENTITY` + 裸 `Id`（不包 Vogen），理由：高寫入、永久保留的日誌筆數可能超過 `int` 範圍，且不需強類型 ID 的保護。一般表的主鍵同樣由資料庫 IDENTITY 配號（§4.1），差別只在型別與是否包 Vogen。
- **§1 的五欄不得省略**：以 `CreateDate`/`CreateUser` 記錄 who/when，`ModifyDate`/`ModifyUser` 與其同值（列不會被更新），`Removed` 恆為 `0`（列不會被軟刪），不得另用 `OperatedBy`/`OperatedAt` 等自訂欄位。與一般表相同套用軟刪除全域過濾器，並以 `.HasComment(...)` 用繁中記錄每欄與表。
- 登入失敗等未驗證操作，`CreateUser` 依 §1 記 `anonymous`；嘗試輸入的帳號只可寫在內容／訊息欄位。
- 可用邏輯關聯（實體名 + 被參照列的 `Id` 字串）取代資料庫外鍵；仍須記 `Id`，不得記業務代碼。
- 任何使用此例外必須在 plan 的 Complexity Tracking 說明，且不得套用於領域聚合。

## 7. 規格與查詢（Ardalis.Specification）

- 以 Specification 封裝查詢邏輯（Where/Search/OrderBy/Include/Skip/Take/Select/AsNoTracking/TagWith/CacheKey）。
- 讀取路徑用 `AsNoTracking`（透過 spec），並宜投影到 DTO 而非materialize完整聚合。
- 禁止 N+1：關聯資料以 `Include`/投影或 spec 載入。
- 分頁用兩個 spec（含/不含 Skip/Take），詳見 `docs/API-CONVENTIONS.md` §4。

```csharp
public sealed class CustomerPageSpec : Specification<Customer, CustomerDto>
{
    public CustomerPageSpec(string keyword, int page, int pageSize)
    {
        Query
            .Where(x => x.IsActive)
            .Search(x => x.Name, $"%{keyword}%")
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CustomerDto(x.Id, x.Name));
    }
}
```

### 條件查詢（Conditional Where）

`.Where()` 接受第二個 `bool condition` 參數：`true` 才套用該過濾，`false` 整條跳過。
適合「選填搜尋欄位」，呼叫端不管傳幾個條件都用同一個 Spec，不需拼 `if`。
可空欄位不需額外 `is not null`，直接用 null-forgiving `!`：

```csharp
public sealed class ProductSearchSpec : Specification<Product>
{
    public ProductSearchSpec(string? name, string? category, decimal? minPrice)
    {
        Query
            .Where(x => x.Name.Contains(name!),
                !string.IsNullOrEmpty(name))          // name 有值才過濾
            .Where(x => x.Category == category!,
                !string.IsNullOrEmpty(category))      // category 有值才過濾
            .Where(x => x.Price >= minPrice!.Value,
                minPrice.HasValue)                    // minPrice 有值才過濾
            .OrderBy(x => x.Name);
    }
}
```

- 條件牽涉 Vogen 強類型 ID 時，LINQ 內不要存取 `.Value`（見 §4.2）：先 `var id = XxxId.From(raw)` 再比較。
- Repository 用法：`ListAsync(spec)` 集合、`FirstOrDefaultAsync(spec)` 單筆、`CountAsync(spec)` 計數、`AnyAsync(spec)` 是否存在。
- 測試可用 `spec.Evaluate(items)` 做記憶體內評估。

## 8. 資料表命名（系統表 vs 業務表）

資料表名一律 **PascalCase 單數**（如 `Order`，非 `Orders`）。是否加 `Sys` 前綴，依「系統／基礎設施」或「業務領域」而定：

- **系統／基礎設施層表 → 加 `Sys` 前綴。** 屬平台性、橫切關注、與具體業務領域無關的資料：身分與存取控制、選單、稽核與日誌等。
  範例：`SysUser`、`SysRole`、`SysUserRole`、`SysTask`（選單）、`SysRoleTask`（角色可見選單）、`SysAuditLog`、`SysAccessLog`。
- **業務領域表 → 不加前綴，直接用領域名。** 屬某業務領域（Bounded Context）的概念，貼近 DDD 通用語言（Ubiquitous Language）。
  範例：`Order`（訂單）、`OrderLine`、`Customer`、`Invoice`。

**判準**：問「這是平台／系統基礎設施，還是某業務領域的概念？」——系統基礎設施用 `Sys`；業務領域概念不加前綴。若一時難以歸類，以「該表是否只為某業務流程存在」判斷：是→業務表（無前綴）。

**適用範圍與細則**：

- 前綴只作用於**資料表名**（EF `ToTable("...")`）。**C# 實體／聚合類別名一律不帶 `Sys`**，一律用領域名（如類別 `User` 對應表 `SysUser`；類別 `Order` 對應表 `Order`）。
- 索引、外鍵、主鍵沿用 EF 預設命名（`IX_`／`FK_`／`PK_` + 表名），前綴自然隨表名一致（如 `IX_Order_OrderNo`、`PK_SysUser`）。
- **更名既有表**必須用**資料保全**的遷移（`RenameTable`／`RenameIndex`），**禁止** drop + create（會遺失資料）。
- 既有系統表維持 `Sys` 前綴，不因本規範回頭更名。

## 9. 其他欄位慣例

- **金額**：`decimal(19,4)`，禁止 `float`／`double`（2026-10-08 裁決，對齊 UDS）；API 依實際值輸出，顯示格式（小數位數）由前端或報表決定。
- **布林欄位**：以 `Is`／`Has` 開頭（如 `IsEnabled`、`IsRead`）；標準欄位 `Removed` 為例外，名稱固定。
- **關鍵字查詢**：清單查詢可用 `Contains`（SQL `LIKE '%kw%'`）；資料量大到影響效能預算時，依憲章原則 VI 實測後再評估索引或全文檢索。
- **Migration 部署**：目前採停機部署，schema 變更可在同一個 migration 內完成，不強制 expand-contract 分階段；上線後若改為不停機部署再重新評估。
- 稽核資料由 EF Core interceptor 寫入，不使用資料庫 trigger。
