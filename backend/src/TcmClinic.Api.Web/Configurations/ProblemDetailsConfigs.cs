using System.Diagnostics;
using System.Text.Json;
using FastEndpoints;
using FluentValidation.Results;

namespace TcmClinic.Api.Web.Configurations;

/// <summary>
/// 所有錯誤回應一律為 RFC 9457 ProblemDetails（docs/API-CONVENTIONS.md §3、DEF-011）。
/// 以全域機制處理整類回應，而非逐一修改端點：空 body 的錯誤狀態碼（404、401、403…）由 StatusCodePages 補上、
/// 未被攔截的例外由 ExceptionHandler 轉成 500、FastEndpoints 驗證器改輸出與 <c>Result.Invalid</c> 相同的 ValidationProblem。
/// </summary>
public static class ProblemDetailsConfigs
{
    private const string ValidationTitle = "欄位驗證失敗";

    // 框架預設的英文標題改為繁中（docs/COMMENTS.md §0）；用例自訂的標題（如「資源狀態衝突」）不受影響。
    private static readonly Dictionary<string, string> LocalizedTitles = new(StringComparer.Ordinal)
    {
        ["Bad Request"] = "請求無效",
        ["Unauthorized"] = "未經授權",
        ["Forbidden"] = "禁止存取",
        ["Not Found"] = "找不到資源",
        ["Method Not Allowed"] = "不允許的請求方法",
        ["An error occurred while processing your request."] = "系統錯誤",
        ["One or more validation errors occurred."] = ValidationTitle,
    };

    public static IServiceCollection AddProblemDetailsConfigs(this IServiceCollection services) =>
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            if (context.ProblemDetails.Title is { } title && LocalizedTitles.TryGetValue(title, out var localized))
            {
                context.ProblemDetails.Title = localized;
            }
        });

    /// <summary>
    /// 例外處理與空 body 補 ProblemDetails。MUST 置於驗證／授權與端點之前，才能涵蓋它們產生的 401、403、404 與例外。
    /// </summary>
    public static WebApplication UseProblemDetailsConfigs(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }

    /// <summary>FastEndpoints 驗證失敗改輸出 ValidationProblem：errors 為「欄位（camelCase）→ 訊息陣列」。</summary>
    public static void UseValidationProblem(this ErrorOptions errors)
    {
        errors.GeneralErrorsField = "error";
        errors.ProducesMetadataType = typeof(HttpValidationProblemDetails);
        errors.ResponseBuilder = (failures, context, statusCode) => ToValidationProblem(failures, context, statusCode);
    }

    private static HttpValidationProblemDetails ToValidationProblem(
        List<ValidationFailure> failures, HttpContext context, int statusCode)
    {
        var errors = failures
            .GroupBy(f => JsonNamingPolicy.CamelCase.ConvertName(f.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        // FastEndpoints 自行序列化回應，不經 IProblemDetailsService，故在此補齊與框架預設相同的欄位。
        context.Response.ContentType = "application/problem+json";
        return new HttpValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = ValidationTitle,
            Status = statusCode,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier },
        };
    }
}
