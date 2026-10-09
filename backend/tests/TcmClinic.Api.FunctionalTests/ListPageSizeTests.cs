using System.Net;
using System.Text.Json;
using static TcmClinic.Api.Tests.Shared.MenuRequestTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 分頁清單的每頁筆數（ADR-007、API-CONVENTIONS §4）：
/// 未指定 pageSize（或 0、負數）時固定 10；指定時依指定值，上限 100。
/// 新增分頁清單時加進 <see cref="Lists"/>（PRJ-2026-0007）。
/// </summary>
public class ListPageSizeTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>分頁清單端點與其選單授權 value。</summary>
    public static TheoryData<string, string> Lists => new()
    {
        { "/roles/maintenance", "rolePermission" },
        { "/users", "accountMaintenance" },
    };

    private async Task<(int PageSize, string? Link)> QueryAsync(string url, string menu)
    {
        var res = await _client.SendAsync(Request(HttpMethod.Get, url, menus: menu), Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(Ct));
        var link = res.Headers.TryGetValues("Link", out var values) ? string.Join(",", values) : null;
        return (doc.RootElement.GetProperty("pageSize").GetInt32(), link);
    }

    private static string WithPageSize(string path, string pageSize) =>
        $"{path}{(path.Contains('?') ? '&' : '?')}page=1&pageSize={pageSize}";

    // 情境（SC-007）：未指定 pageSize、或指定 0、負數時一律回 10，Link 標頭一致。
    [Theory(DisplayName = "未指定或指定非正數的每頁筆數時回 10（ADR-007）")]
    [MemberData(nameof(Lists))]
    public async Task Default_page_size_is_ten(string path, string menu)
    {
        var (pageSize, link) = await QueryAsync(path, menu);
        Assert.Equal(10, pageSize);
        Assert.Contains("pageSize=10", link);

        Assert.Equal(10, (await QueryAsync(WithPageSize(path, "0"), menu)).PageSize);
        Assert.Equal(10, (await QueryAsync(WithPageSize(path, "-5"), menu)).PageSize);
    }

    // 情境：指定 30 依指定值；指定 500 以上限 100 為準。
    [Theory(DisplayName = "指定每頁筆數時依指定值且上限 100（API-CONVENTIONS §4）")]
    [MemberData(nameof(Lists))]
    public async Task Requested_page_size_is_used(string path, string menu)
    {
        Assert.Equal(30, (await QueryAsync(WithPageSize(path, "30"), menu)).PageSize);
        Assert.Equal(100, (await QueryAsync(WithPageSize(path, "500"), menu)).PageSize);
    }
}
