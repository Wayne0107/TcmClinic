# API 慣例（API Conventions）

> 版本 1.5.1｜最後更新 2026-10-09

> 本檔為憲章原則 V（系統一致性）的 **How**，涵蓋端點結構、文件、回應形狀。

## 1. REPR 端點模式

每個端點遵循 Request-Endpoint-Response-Processor：

```
Feature/
  Create.cs                  # 端點處理器
  Create.CreateRequest.cs    # Request DTO
  Create.CreateResponse.cs   # Response DTO
  Create.CreateValidator.cs  # FluentValidation 規則
```

檔案組織與 `Endpoints/` 分組容器見 `docs/CODING-STANDARDS.md` §5。

## 2. API 文件（Swagger/OpenAPI）

每個端點必須在 `Configure()` 提供三個區塊，順序如下：

1. **`Summary(s => { ... })`**：`s.Summary`（一句話）、`s.Description`（含限制的詳述）、`s.ExampleRequest`、`s.ResponseExamples[statusCode]`、`s.Responses[statusCode]`（每個狀態碼的說明）。
2. **`Tags("GroupName")`**：Swagger UI 的邏輯分組。
3. **`Description(builder => ...)`**：`.Accepts<TRequest>(contentType)`、`.Produces<TResponse>(statusCode, contentType)`、`.ProducesProblem(statusCode)`。

所有描述文字使用繁體中文（見 `docs/COMMENTS.md`）。

### 欄位說明（DTO XML 文件註解）

Request／Response DTO（含 UseCases 層直接回傳的 DTO）的**每個欄位**都必須有 XML 文件註解；`TcmClinic.Api.Web` 與 `TcmClinic.Api.UseCases` 已開啟 `GenerateDocumentationFile`，NSwag 會把它帶進 OpenAPI 的 `description`，在 Scalar 顯示於欄位名稱下方。query／route 參數同樣適用。

- **positional record**：在型別上用 `/// <param name="欄位">`；**class 屬性**：在屬性上用 `/// <summary>`。
- 寫給 API 呼叫端看：說明意義、格式／長度、必填或選填、允許值（列舉／代碼）、單位、空值語意。不寫規格編號（FR-xxx、feature 編號）或實作細節；需要保留追溯資訊時放 `<remarks>`。
- 型別本身也加一句 `<summary>`（顯示為 schema 說明）。

```csharp
/// <summary>新增使用者請求。</summary>
/// <param name="Account">登入帳號，最長 50 字；未刪除範圍內唯一（不分大小寫）。</param>
/// <param name="RoleIds">指派的角色 ID 清單，至少一個。</param>
public sealed record CreateUserRequest(string Account, List<int> RoleIds);

public sealed class GetUsersRequest
{
    /// <summary>頁碼，從 1 起算；預設 1。</summary>
    public int Page { get; set; } = 1;
}
```

```csharp
public override void Configure()
{
    Put(UpdateContributorRequest.Route);
    AllowAnonymous();

    Summary(s =>
    {
        s.Summary = "更新貢獻者";
        s.Description = "更新現有貢獻者的資訊。名稱長度必須介於 2 至 100 字元之間。";
        s.ExampleRequest = new UpdateContributorRequest { Id = 1, Name = "Updated Name" };
        s.ResponseExamples[200] = new UpdateContributorResponse(/* ... */);
        s.Responses[200] = "貢獻者更新成功";
        s.Responses[404] = "找不到該貢獻者";
        s.Responses[400] = "輸入資料無效";
    });

    Tags("Contributors");

    Description(builder => builder
        .Accepts<UpdateContributorRequest>("application/json")
        .Produces<UpdateContributorResponse>(200, "application/json")
        .ProducesProblem(404)
        .ProducesProblem(400));
}
```

開發環境暴露：`/swagger`、`/scalar`、`/openapi/{documentName}.json`。

## 3. 回應慣例（Typed Results + Result 模式）

端點必須以 ASP.NET Core **Typed Results**（`Microsoft.AspNetCore.Http.HttpResults`）作為回傳型別，搭配 `Ardalis.Result` 的 pattern matching（透過 `ResultExtensions`）。

### 依操作決定回傳型別

| 操作 | 回傳型別 | 成功 | 失敗 |
|------|---------|------|------|
| Create | `Results<Created<T>, ValidationProblem, ProblemHttpResult>` | 201 + Location + body | 400 / 500 |
| GetById | `Results<Ok<T>, NotFound, ProblemHttpResult>` | 200 + body | 404 / 400 |
| Update | `Results<Ok<T>, NotFound, ValidationProblem, ProblemHttpResult>` | 200 + body | 404 / 400 |
| Delete | `Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>` | 204 | 404 / 400 |
| List | `HandleAsync` + `Send.OkAsync()` | 200 + Link header | 400 |

異動操作（Create/Update/Delete）若命令可能回傳 `Result.Invalid`（業務規則失敗，如「選單仍有子項」），其 `Results<>` union 必須包含 `ValidationProblem`，讓訊息以欄位級呈現。

### Result 到 TypedResults 的對映

端點不得手動 switch `Result.Status`，一律用集中的 `ResultExtensions`（位於 `Web/Extensions/ResultExtensions.cs`）：

- `ToCreatedResult(locationBuilder, responseMapper)` → 201
- `ToGetByIdResult(responseMapper)` → 200 / 404
- `ToUpdateResult(...)` → 200（多載：`Result<T>` 映射值、非泛型 `Result` + 回應工廠、或單純非泛型 `Result` 空 200）
- `ToDeleteResult()` → 204
- `ToNoContentResult()` → 204（非刪除的無內容成功，如密碼重設）
- `ToOkOnlyResult(responseMapper)` → 200（保證恆成功的查詢）
- `ToProblem()` → ProblemDetails 逃生口（成功路徑有副作用而無法用上述 helper，如登入發 cookie）

狀態對映：

| Result.Status | 結果 |
|---------------|------|
| `Ok` | 對應成功 typed result |
| `Invalid` | `TypedResults.ValidationProblem(errors)`（欄位級） |
| `NotFound` | `TypedResults.NotFound()`，空 body 由全域 StatusCodePages 補上 ProblemDetails 404 |
| `Unauthorized` | ProblemDetails 401 |
| `Forbidden` | ProblemDetails 403 |
| `Conflict` | ProblemDetails 409 |
| `CriticalError` | ProblemDetails 500 |
| 其他 | `TypedResults.Problem(title, detail, 400)` |

- 欄位驗證失敗與業務規則違反（含重複鍵，如「帳號已存在」）一律 400 ValidationProblem；只有資源狀態衝突（已被他人處理、rowversion 不符）回 409。不採用 422。

所有錯誤回應一律使用 RFC 9457（2023 年取代 RFC 7807，格式相容）ProblemDetails，透過上述集中對映產生；端點不得自訂臨時錯誤形狀。若因客戶明確需求而偏離標準，必須在程式碼中以規格式註解記錄理由（見 `docs/COMMENTS.md`）。

- 「所有錯誤回應」包含：找不到資源的 404、FastEndpoints 驗證器擋下的 400、驗證／授權中介層回的 401／403，以及未被攔截例外的 500。不得回空 body（2026-10-08 裁決，已實作）。
- 實作集中在 `Configurations/ProblemDetailsConfigs.cs`，以全域機制處理整類回應，端點不需各自處理：
  - `AddProblemDetails()`：框架預設的英文標題改為繁中（如「找不到資源」「未經授權」「欄位驗證失敗」），並自動附 `traceId`，可用來對照日誌。
  - `UseExceptionHandler()`：未被攔截的例外轉成 500 ProblemDetails，不外洩例外訊息。
  - `UseStatusCodePages()`：空 body 的錯誤狀態碼（404、401、403 等）補上 ProblemDetails。兩者 MUST 置於驗證／授權與端點之前。
  - FastEndpoints 驗證器的 400 改輸出與 `Result.Invalid` 相同的 ValidationProblem：`errors` 為「欄位（camelCase）→ 訊息陣列」；無欄位的一般錯誤鍵名為 `error`。

### Mapper 模式

- 端點直接映射領域實體時，用 FastEndpoints `Mapper<TRequest, TResponse, TEntity>`，於 `ExecuteAsync` 以 `Map.FromEntity` 取用。
- 當 UseCases 已回傳 DTO（本範本預設），傳給 helper 的 `responseMapper` 可為 inline lambda（如 `nodes => nodes`、`id => new CreateResponse(id)`）。

### List 端點特別處理

List 端點用 `HandleAsync`（非 `ExecuteAsync` + `Results<>`），因為要另外注入 GitHub 風格分頁 **Link header**（`first`/`prev`/`next`/`last`）。

範例（Create）：

```csharp
public override async Task<Results<Created<CreateResponse>, ValidationProblem, ProblemHttpResult>>
  ExecuteAsync(CreateRequest request, CancellationToken ct)
{
    var result = await _mediator.Send(new CreateCommand(Name.From(request.Name!)));
    return result.ToCreatedResult(
        id => $"/Contributors/{id}",
        id => new CreateResponse(id.Value, request.Name!));
}
```

## 4. 分頁與效能

- List 端點必須分頁並強制最大頁數上限（預設 100），禁止無界結果集。
- 每頁筆數：請求的 `pageSize` 宣告為可空（`int?`），不在 request DTO 設初始值（FastEndpoints 只繫結出現的查詢參數，DTO 初始值會在進入 handler 前生效）；未指定、或指定 0 或負數時為 10；指定時依指定值，超過上限以上限為準。handler 統一使用 `PageSizeResolver.Resolve`（`TcmClinic.Api.UseCases.Paging`），不在各清單寫死，也不讀設定檔或資料庫參數。回應與 `Link` 標頭使用實際採用的每頁筆數。依據為 [ADR-007](adr/ADR-007-default-page-size-fixed.md)。
- 分頁用兩個 Spec：一個含 Skip/Take 取資料、一個不含以取總數。
- 效能預算與資料存取細節見 `docs/CODING-STANDARDS.md` 與憲章原則 VI。
- 回應形狀固定為 `{ items, page, pageSize, totalCount }` ＋ `Link` 標頭；採 offset 分頁，不包 `{ data, meta, links }` 信封，不用 `limit` 參數名。
- **分頁策略**（2026-10-08 使用者決定）：原則上一律用上述 offset 分頁，不用 keyset。清單出現效能需求（如持續增長的紀錄或日誌、深頁查詢變慢）時，再於該清單的規格評估改用 keyset（請求 `cursor`＋`pageSize`，回應 `{ items, pageSize, nextCursor, hasMore }`，排序鍵須唯一且有索引）；改動會變更 API 契約，須先告知前端。

## 5. 路由與版本

- 資源用複數 kebab-case（如 `/users`、`/roles/{id}/menu-visibility`）。
- 狀態轉換類動作允許 `POST /{resource}/{id}/{action}`（如 `/orders/{id}/approve`、`/reject`）；登入、登出等非資源操作沿用 `/auth/login`、`/auth/logout`；以登入者為對象的單數資源允許（如 `/profile`、`/me/menus`）。
- 識別碼一律用對外識別碼 `PublicId`（UUIDv7），不公開資料表的整數主鍵（SPEC-064，2026-10-08）：路徑寫 `{id:guid}`，格式不符或查無資料回 404 ProblemDetails；回應與請求內容中的識別碼與參照（`id`、`roleIds`、`parentId` 等）都是 UUID 字串，欄位名稱照舊。端點收原始 `Guid`，用例以 `XxxPublicId.TryFrom` 轉成強型別後以 `XxxByPublicIdSpec` 查詢，請求內容中的參照先換成內部主鍵再處理。以業務代碼識別的資源（如參數類代碼表）可維持用代碼，須於該規格註明。Swagger 範例值放在 `Endpoints/ExampleIds.cs`。
- 驗證（2026-10-08，ADR-011）：受保護端點一律以 `Authorization: Bearer <access token>` 呼叫；access token 由 `POST /auth/login`、`POST /auth/refresh` 取得，refresh token 只以 HttpOnly Cookie 往返。未帶或帶無效 token 回 401 ProblemDetails。新端點不得改用 Cookie 或查詢參數傳遞 token。
- 不做 URL 版本（無 `/v1/` 前綴）：前後端同步部署。
- 不採用錯誤代碼體系（`<PREFIX>_<CATEGORY>_<NUMBER>`）：錯誤以 ProblemDetails 的 `title`／`detail`／`errors` 表達。若日後要導入，須另開規格全面處理，不得逐功能零星加入。
- 速率限制回應標頭（`X-RateLimit-*`）不採用。只有 `POST /auth/login` 依來源 IP 限速（每分鐘 20 次，設定檔 `LoginRateLimit`），超過回 429 ProblemDetails 並附 `Retry-After`（SPEC-004 FR-006b）；需要限速的端點以 `RequireRateLimiting(<原則名>)` 掛上 `RateLimitConfigs` 定義的原則。
