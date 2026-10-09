using System.Text.Json;

namespace TcmClinic.Api.Tests.Shared;

/// <summary>錯誤回應（ProblemDetails）的功能與驗收測試共用讀取工具。</summary>
internal static class ProblemDetailsTestSupport
{
    /// <summary>
    /// 讀取 400 回應的欄位錯誤（ValidationProblem 的 `errors` 物件）。FastEndpoints 驗證器與 Result.Invalid
    /// 已統一為 camelCase 欄位名（DEF-011），仍以不分大小寫的字典回傳，讓測試不受欄位名大小寫影響。
    /// </summary>
    public static async Task<Dictionary<string, string[]>> ErrorsAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = doc.RootElement.EnumerateObject()
            .First(p => string.Equals(p.Name, "errors", StringComparison.OrdinalIgnoreCase)).Value;
        return errors.EnumerateObject().ToDictionary(
            p => p.Name,
            p => p.Value.EnumerateArray().Select(v => v.GetString() ?? string.Empty).ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }
}
