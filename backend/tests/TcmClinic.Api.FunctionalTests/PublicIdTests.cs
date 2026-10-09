using System.Net;
using System.Text.Json;
using static TcmClinic.Api.Tests.Shared.MenuRequestTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 對外識別碼（SPEC-064）：API 的識別碼一律為 UUIDv7（PublicId），不再公開整數主鍵；
/// 以整數或格式錯誤的識別碼呼叫路徑含 {id} 的端點回 404。
/// </summary>
public class PublicIdTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string AllMenus = "accountMaintenance,rolePermission,/admin/menus";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        var res = await factory.CreateClient().SendAsync(Request(HttpMethod.Get, url, menus: AllMenus), Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.Clone();
    }

    /// <summary>收集 JSON 中所有名為 id 的值（含巢狀子節點）。</summary>
    private static List<JsonElement> Ids(JsonElement element)
    {
        var ids = new List<JsonElement>();
        void Walk(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in e.EnumerateObject())
                {
                    if (p.NameEquals("id"))
                    {
                        ids.Add(p.Value);
                    }

                    Walk(p.Value);
                }
            }
            else if (e.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in e.EnumerateArray())
                {
                    Walk(item);
                }
            }
        }

        Walk(element);
        return ids;
    }

    // 情境：使用者、角色、選單的清單回應中，所有 id 都是 UUIDv7 字串（種子資料於啟動時以實體建構，皆為 v7）。
    // @SPEC-064 @AC-1
    [Theory(DisplayName = "清單回應中的 id 皆為 UUIDv7（SPEC-064 AC-1）")]
    [InlineData("/users")]
    [InlineData("/roles")]
    [InlineData("/roles/maintenance")]
    [InlineData("/menus")]
    public async Task Responses_use_uuid_v7_ids(string url)
    {
        var ids = Ids(await GetJsonAsync(url));

        Assert.NotEmpty(ids);
        Assert.All(ids, id =>
        {
            Assert.Equal(JsonValueKind.String, id.ValueKind);
            Assert.Equal(7, Guid.Parse(id.GetString()!).Version);
        });
    }

    // 情境：以清單取得的角色 id 呼叫可見選單端點成功，回應的選單節點 id 同樣為 UUID。
    // @SPEC-064 @AC-2
    [Fact(DisplayName = "以清單取得的 id 操作資料成功（SPEC-064 AC-2）")]
    public async Task Operations_accept_public_id()
    {
        var roles = await GetJsonAsync("/roles");
        var roleId = roles.EnumerateArray().First().GetProperty("id").GetString();

        var visibility = await GetJsonAsync($"/roles/{roleId}/menu-visibility");

        Assert.All(Ids(visibility), id => Guid.Parse(id.GetString()!));
    }

    // 情境：以整數主鍵或非 UUID 字串呼叫路徑含 {id} 的端點，視同不存在的資源回 404 ProblemDetails。
    // 方法須是該路徑實際存在的方法：路由先比對 HTTP 方法，方法不存在時回 405，不會走到 {id:guid} 約束。
    // @SPEC-064 @AC-3
    [Theory(DisplayName = "整數或格式錯誤的識別碼回 404（SPEC-064 AC-3）")]
    [InlineData("PUT", "/users/1")]
    [InlineData("GET", "/roles/1/menu-visibility")]
    [InlineData("PUT", "/users/not-a-guid")]
    public async Task Integer_or_malformed_id_returns_404(string method, string url)
    {
        var res = await factory.CreateClient().SendAsync(Request(new HttpMethod(method), url, menus: AllMenus), Ct);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }
}
