using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace TcmClinic.Api.Web.Extensions;

/// <summary>
/// 集中將 Ardalis <see cref="Result"/>／<see cref="Result{T}"/> 對應為 ASP.NET Core Typed Results：
/// 成功走對應 2xx 型別結果，失敗走 RFC 9457 ProblemDetails（見 docs/API-CONVENTIONS.md §3）。
/// <c>NotFound</c> 回 <see cref="TypedResults.NotFound()"/>（空 body），由全域 StatusCodePages 補上 ProblemDetails（ProblemDetailsConfigs）。
/// 命名與結構參考 Ardalis Clean Architecture 範本的 ResultExtensions；並針對本專案「非泛型 Result 的
/// 更新／刪除」與「登入 401」補上必要多載。端點 MUST 透過本類別回應，MUST NOT 於端點內手動
/// switch <see cref="ResultStatus"/> 或自訂錯誤格式。
/// </summary>
public static class ResultExtensions
{
    /// <summary>Create → 201 Created + Location + body（Invalid→ValidationProblem；其餘→ProblemDetails）。</summary>
    public static Results<Created<TResponse>, ValidationProblem, ProblemHttpResult>
        ToCreatedResult<TValue, TResponse>(
            this Result<TValue> result,
            Func<TValue, string> locationBuilder,
            Func<TValue, TResponse> mapResponse) => result.Status switch
            {
                ResultStatus.Ok => TypedResults.Created(
                    locationBuilder(result.Value), mapResponse(result.Value)),
                ResultStatus.Invalid => ToValidationProblem(result.ValidationErrors),
                _ => ToProblem(result.Status, result.Errors, "建立失敗"),
            };

    /// <summary>GetById／單筆查詢 → 200 OK + body（NotFound→404；其餘→ProblemDetails）。</summary>
    public static Results<Ok<TResponse>, NotFound, ProblemHttpResult>
        ToGetByIdResult<TValue, TResponse>(
            this Result<TValue> result,
            Func<TValue, TResponse> mapResponse) => result.Status switch
            {
                ResultStatus.Ok => TypedResults.Ok(mapResponse(result.Value)),
                ResultStatus.NotFound => TypedResults.NotFound(),
                _ => ToProblem(result.Status, result.Errors, "查詢失敗"),
            };

    /// <summary>Update（Result 帶值）→ 200 OK + body（NotFound→404；Invalid→ValidationProblem；其餘→ProblemDetails）。</summary>
    public static Results<Ok<TResponse>, NotFound, ValidationProblem, ProblemHttpResult>
        ToUpdateResult<TValue, TResponse>(
            this Result<TValue> result,
            Func<TValue, TResponse> mapResponse) => result.Status switch
            {
                ResultStatus.Ok => TypedResults.Ok(mapResponse(result.Value)),
                ResultStatus.NotFound => TypedResults.NotFound(),
                ResultStatus.Invalid => ToValidationProblem(result.ValidationErrors),
                _ => ToProblem(result.Status, result.Errors, "更新失敗"),
            };

    /// <summary>Update（非泛型 Result，body 由請求提供）→ 200 OK + body。</summary>
    public static Results<Ok<TResponse>, NotFound, ValidationProblem, ProblemHttpResult>
        ToUpdateResult<TResponse>(
            this Result result,
            Func<TResponse> mapResponse) => result.Status switch
            {
                ResultStatus.Ok => TypedResults.Ok(mapResponse()),
                ResultStatus.NotFound => TypedResults.NotFound(),
                ResultStatus.Invalid => ToValidationProblem(result.ValidationErrors),
                _ => ToProblem(result.Status, result.Errors, "更新失敗"),
            };

    /// <summary>Update（非泛型 Result，無 body）→ 200 OK。</summary>
    public static Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>
        ToUpdateResult(this Result result) => result.Status switch
        {
            ResultStatus.Ok => TypedResults.Ok(),
            ResultStatus.NotFound => TypedResults.NotFound(),
            ResultStatus.Invalid => ToValidationProblem(result.ValidationErrors),
            _ => ToProblem(result.Status, result.Errors, "更新失敗"),
        };

    /// <summary>Delete／其他無內容成功 → 204 No Content（NotFound→404；Invalid→ValidationProblem；其餘→ProblemDetails）。</summary>
    public static Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>
        ToDeleteResult(this Result result) => result.Status switch
        {
            ResultStatus.Ok => TypedResults.NoContent(),
            ResultStatus.NotFound => TypedResults.NotFound(),
            ResultStatus.Invalid => ToValidationProblem(result.ValidationErrors),
            _ => ToProblem(result.Status, result.Errors, "刪除失敗"),
        };

    /// <summary>非刪除但成功回無內容（例：重設密碼）→ 204 No Content。語意同 <see cref="ToDeleteResult"/>。</summary>
    public static Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>
        ToNoContentResult(this Result result) => result.Status switch
        {
            ResultStatus.Ok => TypedResults.NoContent(),
            ResultStatus.NotFound => TypedResults.NotFound(),
            ResultStatus.Invalid => ToValidationProblem(result.ValidationErrors),
            _ => ToProblem(result.Status, result.Errors, "作業失敗"),
        };

    /// <summary>
    /// 帶篩選參數、可能驗證失敗的查詢（SPEC-014，例：分行選項清單）→ 200 OK + body
    /// （Invalid→ValidationProblem；其餘→ProblemDetails）。
    /// </summary>
    public static Results<Ok<TResponse>, ValidationProblem, ProblemHttpResult>
        ToQueryResult<TValue, TResponse>(
            this Result<TValue> result,
            Func<TValue, TResponse> mapResponse) => result.Status switch
            {
                ResultStatus.Ok => TypedResults.Ok(mapResponse(result.Value)),
                ResultStatus.Invalid => ToValidationProblem(result.ValidationErrors),
                _ => ToProblem(result.Status, result.Errors, "查詢失敗"),
            };

    /// <summary>List／固定成功查詢 → 200 OK + body（呼叫端須確保 Result 恆為成功）。</summary>
    public static Ok<TResponse> ToOkOnlyResult<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> mapResponse) => TypedResults.Ok(mapResponse(result.Value));

    /// <summary>
    /// 供「成功路徑另有副作用、無法套用上述 helper」的端點（例：登入簽發 Cookie）將失敗對應為 ProblemDetails。
    /// 依 <see cref="ResultStatus"/> 對應狀態碼（Unauthorized→401、Forbidden→403、NotFound→404、Conflict→409，其餘→400）。
    /// </summary>
    public static ProblemHttpResult ToProblem<T>(this Result<T> result) =>
        ToProblem(result.Status, result.Errors, "請求失敗");

    /// <inheritdoc cref="ToProblem{T}(Result{T})"/>
    public static ProblemHttpResult ToProblem(this Result result) =>
        ToProblem(result.Status, result.Errors, "請求失敗");

    private static ProblemHttpResult ToProblem(
        ResultStatus status, IEnumerable<string> errors, string fallbackTitle) => status switch
        {
            ResultStatus.Unauthorized => TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized, title: "未經授權"),
            ResultStatus.Forbidden => TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden, title: "禁止存取"),
            ResultStatus.NotFound => TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound, title: "找不到資源"),
            // detail 帶出用例的衝突訊息（如受理開狀申請書「該申請已被處理。」，FR-011），供前端直接顯示。
            ResultStatus.Conflict => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "資源狀態衝突",
                detail: errors.Any() ? string.Join("; ", errors) : null),
            // SPEC-013（FR-016）：用例已攔截的非預期錯誤 → 500，detail 為可直接顯示給使用者的訊息（不含例外細節）。
            ResultStatus.CriticalError => TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "系統錯誤",
                detail: errors.Any() ? string.Join("; ", errors) : null),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: fallbackTitle,
                detail: errors.Any() ? string.Join("; ", errors) : null),
        };

    private static ValidationProblem ToValidationProblem(IEnumerable<ValidationError> errors) =>
        TypedResults.ValidationProblem(errors
            .GroupBy(e => string.IsNullOrEmpty(e.Identifier) ? "error" : e.Identifier)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? string.Empty).ToArray()));
}
