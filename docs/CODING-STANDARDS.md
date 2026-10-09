# 編碼規範（Coding Standards）

> 版本 1.1.1｜最後更新 2026-10-08

> 本檔為憲章原則 III（架構一致性）、IV（類型安全）、V（系統一致性）的 **How**。
> 憲章定義「應當如何裁決」，本檔定義「如何實作」。兩者衝突時以憲章為準。

## 1. Clean Architecture 分層

依賴永遠指向內層；外層依賴內層，內層不得引用外層。

| 層 | 內容 | 相依 |
|----|------|------|
| **Core** | 領域實體、聚合、值物件、領域事件、規格、介面 | 零外部框架相依 |
| **UseCases** | 應用邏輯（命令／查詢）；只有 use case 需要的介面（如 `ILoginThrottle`、查詢服務） | 僅依賴 Core |
| **Infrastructure** | EF Core、外部服務、email、檔案存取 | 實作 Core 或 UseCases 定義的介面 |
| **Web** | API 端點、Request/Response DTO、輸入驗證 | 透過 DI 依賴 UseCases 與 Infrastructure |

- 介面放在需要它的最內層：Core 需要就放 Core；只有 UseCases 需要就放 UseCases，與使用它的 use case 同資料夾（沿用 Ardalis Clean Architecture 範本做法）。
- 違反依賴方向 = Code Review BLOCKER，並由 NetArchTest 自動擋下（見 `docs/TESTING.md`）。

## 2. Domain-Driven Design（DDD）

- 實體採封裝，盡量減少 public setter。
- 實體變更方法必須回傳 `this` 以支援流暢串接：
  `contributor.UpdateName(name).UpdatePhoneNumber(phone)`。
- 以清楚邊界將相關實體組成聚合（Aggregate）。
- 無識別的概念用值物件（Vogen），驗證邏輯寫在其 `Validate` 內。
- 主鍵用強類型 ID（見 §4）。
- 需要行為/多型的列舉用 `Ardalis.SmartEnum`，不用原生 `enum`。
- 跨聚合溝通用領域事件（透過 `SaveChangesInterceptor` 自動派發）。
- Repository 介面定義在 Core，實作在 Infrastructure。
- 領域不變式必須由模型自身強制，不可表達的狀態必須無法被建立。

### 領域事件派發（Interceptor 模式）

領域事件必須透過 EF Core `SaveChangesInterceptor`（`EventDispatchInterceptor`）自動派發：

- 實體以 `RegisterDomainEvent()` 註冊領域事件。
- Interceptor 在 `SaveChangesAsync` 成功**之後**收集並派發事件——確保事件只在持久化成功後才觸發，杜絕忘記發布。
- 事件處理器透過 DI 解析，由 `IDomainEventDispatcher`（實作為 `MediatorDomainEventDispatcher`）呼叫。
- 處理器**不得**在同一交易內修改其他無關聚合；需要時用最終一致性或開新 scope。

## 3. CQRS 與 Mediator

- Command（異動）必須透過 Repository 抽象存取資料。
- Query（讀取）可繞過 Repository 以最佳化（直接 SQL、查詢服務、規格）。
- 使用 Mediator source generator 派發命令/查詢。
- 橫切關注點（日誌、驗證、快取）以 pipeline behavior 實作。
- 命令/查詢參數優先用值物件；端點層負責把原始型別轉為值物件再派發。

## 4. 強類型 ID（Vogen）

所有實體主鍵必須用 Vogen 值物件，禁止裸原始型別：

```csharp
[ValueObject<int>]
public readonly partial struct ContributorId
{
    private static Validation Validate(int value)
        => value >= 0
            ? Validation.Ok
            : Validation.Invalid("ContributorId 必須為非負數。");
}
```

- 實體基底：`EntityBase<TEntity, TId>`。
- 主鍵 ID 的驗證不得限制正負：資料庫 IDENTITY 配號前，EF Core 會指派負數暫時值（見 `docs/DATABASE-SCHEMA.md` §4.1、ADR-010）。
- 端點層接收原始 `int`/`string`，轉為強類型 ID 後再派發命令。
- EF Core 整合規則（IDENTITY 配號、LINQ 限制、HasVogenConversion）見 `docs/DATABASE-SCHEMA.md`。

## 5. 檔案組織（功能優先）

程式碼依 **功能／聚合** 組織，不依技術層級橫向切割。

REPR 端點切片：

```
Contributors/
  Create.cs                  # 端點處理器
  Create.CreateRequest.cs    # Request DTO
  Create.CreateResponse.cs   # Response DTO
  Create.CreateValidator.cs  # FluentValidation 規則
```

- 禁止 `Controllers/`、`Models/`、`Services/` 這類橫向資料夾。
- Web 專案中，各功能切片資料夾必須統一收在單一 `Endpoints/` 容器下：

```
{Project}.Web/
  Endpoints/
    Menus/        # Create.cs, Update.cs, ... （完整 REPR 切片）
    Roles/
  Configurations/ # 跨切面接線（auth、DI、Swagger）
  Program.cs
```

`Endpoints/` 是分組容器，不是橫向切割——其每個子資料夾仍是完整功能切片。

## 6. Web 設定組織

Web 專案的啟動設定收在 `Configurations/`，每個都是 `static class` 擴充方法：

```
Configurations/
  LoggerConfigs.cs       # Serilog 設定
  MediatorConfig.cs      # Mediator source-gen 註冊
  MiddlewareConfig.cs    # 中介軟體管線 + DB seeding
  OptionConfigs.cs       # IOptions 綁定
  ServiceConfigs.cs      # DI 註冊
```

## 7. 命名規範

| 元素 | 規則 | 範例 |
|------|------|------|
| 類型／方法 | PascalCase | `CreateContributorHandler` |
| async 方法 | `Async` 後綴 | `GetByIdAsync` |
| 參數 | camelCase | `contributorId` |
| 私有欄位 | `_camelCase` | `_repository` |
| 常數 | ALL_UPPER | `MAX_RETRY`、`DEFAULT_PAGE_SIZE` |
| 介面 | `I` 前綴 | `IContributorRepository` |

## 8. 格式與語言慣例

- **檔案範圍命名空間**（file-scoped namespace）必須使用（warning 級強制）。
- **縮排**：C# 4 空格；`.csproj`/`.props`/`.json`/`.yml` 2 空格。
- **括號風格**：Allman——開括號自成一行。
- **修飾符順序**：`public, private, protected, internal, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, volatile, async`。
- 非介面成員必須明確宣告存取修飾符。
- 優先 `readonly struct` / `readonly` 欄位；優先 static 匿名/區域函式。
- **表達式主體成員**：僅允許屬性、存取子、索引子、Lambda；禁止建構式、方法、運算子、區域函式。
- **Pattern matching**：優先 `is not null`、`not` 模式、`switch` 運算式、`??`、`?.`、`is null`。
- **括號**：算術/二元/關係運算一律加括號；不必要的括號一律省略。
- **using**：不分組、不將 System 排最前、置於命名空間外。
- 型別明顯時優先 `var`。

## 9. Primary Constructor

優先使用 primary constructor，但**必須指派到私有欄位**，不可直接使用參數：

```csharp
public class CreateHandler(IRepository<Contributor> repository)
{
    private readonly IRepository<Contributor> _repository = repository; // ✅

    public Task Handle() => _repository.AddAsync(/* ... */);
}
```

## 10. 其他

- **YAGNI**：不加入當下不需要的抽象、功能或基礎設施。
- `.editorconfig`（repo 根目錄）為所有格式規則的唯一權威來源；程式碼不得以本地 editorconfig 覆蓋。
- 一致性檢查：`dotnet format --verify-no-changes`；自動修復：`dotnet format`。
