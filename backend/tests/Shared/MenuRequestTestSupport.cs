using System.Net.Http.Json;

namespace TcmClinic.Api.Tests.Shared;

/// <summary>
/// 功能與驗收測試共用：建立帶「可見選單」標頭的請求，由 <see cref="TestAuthHandler"/> 轉為 menu claims，
/// 讓端點的 <c>MenuAccess:&lt;value&gt;</c> 授權通過（驗證替換、授權真實，docs/TESTING.md）。
/// </summary>
internal static class MenuRequestTestSupport
{
    /// <summary>建立帶可見選單標頭的請求；<paramref name="menus"/> 為逗號分隔的選單 value。</summary>
    public static HttpRequestMessage Request(HttpMethod method, string url, string menus, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add(TestAuthHandler.MenusHeader, menus);
        if (body is not null)
        {
            req.Content = JsonContent.Create(body);
        }

        return req;
    }
}
