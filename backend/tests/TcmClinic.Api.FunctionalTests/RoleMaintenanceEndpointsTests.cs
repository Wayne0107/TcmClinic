using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 角色權限維護清單與新增端點的功能測試（SPEC-012，US1/US2；對真實 SQL 容器）。
/// </summary>
public class RoleMaintenanceEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public ValueTask InitializeAsync() => new(RoleTestSupport.EnsureSampleRolesAsync(factory));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed record MaintItem(
        Guid Id, string Code, string Name, string? Description,
        string RoleType, bool IsAssigned, string RowVersion);

    private sealed record MaintList(MaintItem[] Items, int Page, int PageSize, int TotalCount);

    private sealed record CreatedDto(Guid Id);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<MaintList> MaintenanceAsync(int page = 1, int pageSize = 100) =>
        (await _client.GetFromJsonAsync<MaintList>(
            $"/roles/maintenance?page={page}&pageSize={pageSize}", Ct))!;

    // 情境（US1）：維護清單回傳角色種類、是否已指派與分頁資訊（FR-001/002/003/004、SC-002）。
    // @SPEC-012 @AC-2
    // @SPEC-012 @AC-4
    [Fact(DisplayName = "維護清單含角色種類/是否已指派/分頁（012，FR-002/003/004）")]
    public async Task Maintenance_list_includes_type_assigned_and_paging()
    {
        var list = await MaintenanceAsync();

        Assert.Equal(1, list.Page);
        Assert.True(list.TotalCount >= 1 + RoleTestSupport.SampleRoleCount); // SUPERADMIN + 範例角色
        Assert.Equal(Math.Min(list.TotalCount, list.PageSize), list.Items.Length);

        var sm = list.Items.Single(i => i.Code == RoleTestSupport.SystemRoleCode);
        Assert.Equal("System", sm.RoleType);

        var superAdmin = list.Items.Single(i => i.Code == "SUPERADMIN");
        Assert.Equal("Custom", superAdmin.RoleType);
        Assert.True(superAdmin.IsAssigned); // 初始 admin 使用者指派 SUPERADMIN

        Assert.All(list.Items, i => Assert.False(string.IsNullOrEmpty(i.RowVersion)));
    }

    // 情境（US1/AC1）：每頁筆數小於總數時，第 1 頁筆數＝pageSize、totalCount 為全部筆數，第 2 頁為不同角色。
    // @SPEC-012 @AC-1
    [Fact(DisplayName = "維護清單分頁：第 1 頁筆數等於 pageSize 且第 2 頁為後續角色（012，US1/AC1）")]
    public async Task Maintenance_list_pages_by_page_size()
    {
        var all = await MaintenanceAsync();

        var first = await MaintenanceAsync(page: 1, pageSize: 2);
        var second = await MaintenanceAsync(page: 2, pageSize: 2);

        Assert.Equal(2, first.PageSize);
        Assert.Equal(2, first.Items.Length);
        Assert.Equal(all.TotalCount, first.TotalCount);
        Assert.Equal(all.Items.Take(2).Select(i => i.Id), first.Items.Select(i => i.Id));
        Assert.Equal(all.Items.Skip(2).Take(2).Select(i => i.Id), second.Items.Select(i => i.Id));
    }

    // 情境（Edge Case 1）：頁碼超出總頁數 → 回空頁，但 totalCount 仍正確（前端可回退至有效頁）。
    // @SPEC-012 @AC-19
    [Fact(DisplayName = "維護清單頁碼超出總頁數應回空清單但 totalCount 正確（012，Edge Case）")]
    public async Task Maintenance_list_out_of_range_page_returns_empty_items()
    {
        var all = await MaintenanceAsync();

        var beyond = await MaintenanceAsync(page: 9999, pageSize: 10);

        Assert.Empty(beyond.Items);
        Assert.Equal(9999, beyond.Page);
        Assert.Equal(all.TotalCount, beyond.TotalCount);
    }

    // 情境（US1/AC3）：未被任何使用者指派的角色 → isAssigned=false。
    // @SPEC-012 @AC-3
    [Fact(DisplayName = "未被指派的角色於維護清單標示 isAssigned=false（012，US1/AC3）")]
    public async Task Unassigned_role_is_marked_not_assigned()
    {
        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code = "MT_UNASG", name = "未指派角色", description = (string?)null, roleType = "Custom" },
            Ct);
        response.EnsureSuccessStatusCode();

        var list = await MaintenanceAsync();
        var role = list.Items.Single(i => i.Code == "MT_UNASG");
        Assert.False(role.IsAssigned);
    }

    // 情境（FR-014/SC-005）：僅具其他選單（帳號維護）、無 rolePermission 者，對角色維護各操作一律 403；
    // 2026-10-02 舊後台選單 /admin/roles 移除，PUT /roles/{id} 一併改以 rolePermission 授權。
    // @SPEC-012 @AC-16
    [Theory(DisplayName = "無 rolePermission 者呼叫角色維護操作應回 403（012，FR-014/SC-005）")]
    [InlineData("GET", "/roles/maintenance?page=1&pageSize=10")]
    [InlineData("POST", "/roles")]
    [InlineData("GET", "/roles/0199c3a1-7b2e-7c4d-9a10-2f3b4c5d6e00/menu-visibility")]
    [InlineData("PUT", "/roles/0199c3a1-7b2e-7c4d-9a10-2f3b4c5d6e00/menu-visibility")]
    [InlineData("PUT", "/roles/0199c3a1-7b2e-7c4d-9a10-2f3b4c5d6e00")]
    [InlineData("DELETE", "/roles/0199c3a1-7b2e-7c4d-9a10-2f3b4c5d6e00")]
    public async Task Maintenance_ops_without_role_permission_return_403(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Headers.Add(TestAuthHandler.MenusHeader, "accountMaintenance");
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new
            {
                name = "無權新增",
                roleType = "Custom",
                grantedMenuItemIds = Array.Empty<Guid>(),
                rowVersion = "AAAAAAAAB9E=",
            });
        }

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // 情境（FR-014）：僅具 rolePermission 者可新增角色（授權以 rolePermission 為準）。
    // @SPEC-012 @AC-16
    [Fact(DisplayName = "僅具 rolePermission 者可新增角色（012，FR-014）")]
    public async Task Create_with_role_permission_only_succeeds()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/roles")
        {
            Content = JsonContent.Create(new { name = "僅維護權限新增", description = (string?)null, roleType = "Custom" }),
        };
        request.Headers.Add(TestAuthHandler.MenusHeader, "rolePermission");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // 情境（US1，SC-005）：無 rolePermission 選單權限者呼叫維護清單 → 403。
    // @SPEC-012 @AC-16
    [Fact(DisplayName = "無 rolePermission 權限呼叫維護清單應回 403（012，FR-014/SC-005）")]
    public async Task Maintenance_list_without_permission_returns_403()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/roles/maintenance?page=1&pageSize=10");
        request.Headers.Add(TestAuthHandler.MenusHeader, "accountMaintenance"); // 有其他頁權限，但無 rolePermission
        var response = await _client.SendAsync(request, Ct);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // 情境（US2）：不帶代碼新增 → 後端自動產生 R### 且可於清單查得（FR-005/006/007）。
    // @SPEC-012 @AC-5
    [Fact(DisplayName = "不帶代碼新增角色應自動產生 R### 代碼（012，FR-007）")]
    public async Task Create_without_code_autogenerates_r_code()
    {
        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code = (string?)null, name = "自動產碼測試", description = (string?)null, roleType = "Custom" },
            Ct);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedDto>(Ct);

        var list = await MaintenanceAsync();
        var role = list.Items.Single(i => i.Id == created!.Id);
        Assert.Matches(new Regex("^R\\d{3,}$"), role.Code);
        Assert.Equal("Custom", role.RoleType);
    }

    // 情境（US2）：帶代碼新增仍相容（向後相容既有呼叫）。
    [Fact(DisplayName = "帶代碼新增角色仍相容（012 向後相容）")]
    public async Task Create_with_explicit_code_still_works()
    {
        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code = "MT_EXP", name = "顯式代碼", description = (string?)null, roleType = "Custom" },
            Ct);
        response.EnsureSuccessStatusCode();

        var list = await MaintenanceAsync();
        Assert.Contains(list.Items, i => i.Code == "MT_EXP" && i.RoleType == "Custom");
    }

    // 情境（US2/AC4，FR-004）：新增角色僅能為 Custom；送 System → 400 指出 roleType，且不建立（System 僅來自種子）。
    // @SPEC-012 @AC-8
    [Fact(DisplayName = "新增系統管理員類（System）角色應回 400 且不建立（012，US2/AC4、FR-004）")]
    public async Task Create_with_system_role_type_returns_400_and_does_not_create()
    {
        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code = "MT_SYS", name = "不可建System", description = (string?)null, roleType = "System" },
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains("roleType", body, StringComparison.OrdinalIgnoreCase);

        var list = await MaintenanceAsync();
        Assert.DoesNotContain(list.Items, i => i.Code == "MT_SYS");
    }

    // 情境（US2）：角色種類非法 → 400。
    // @SPEC-012 @AC-7
    [Fact(DisplayName = "角色種類非法新增應回 400（012，FR-005）")]
    public async Task Create_with_invalid_role_type_returns_400()
    {
        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code = (string?)null, name = "壞種類", description = (string?)null, roleType = "Nope" },
            Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 情境（US2/AC3，FR-005）：缺必填（名稱／種類）或名稱、說明超長 → 400 並指出欄位（不得 500）。
    // @SPEC-012 @AC-7
    [Theory(DisplayName = "新增角色缺必填或超長應回 400 並指出欄位（012，US2/AC3）")]
    [InlineData("", "Custom", null, "name")]
    [InlineData("   ", "Custom", null, "name")]
    [InlineData("一二三四五六七八九十一二三四五六七八九十一", "Custom", null, "name")]
    [InlineData("缺種類", null, null, "roleType")]
    [InlineData("說明超長", "Custom", "LONG", "description")]
    public async Task Create_with_missing_or_invalid_fields_returns_400(
        string name, string? roleType, string? description, string field)
    {
        var desc = description == "LONG" ? new string('說', 201) : description;

        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code = (string?)null, name, description = desc, roleType },
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains(field, body, StringComparison.OrdinalIgnoreCase);
    }

    // 情境（設計決策 R5）：自動產碼跨已軟刪除角色掃描——刪除最新 R### 後再新增，不得回收該代碼。
    // @SPEC-012 @AC-18
    [Fact(DisplayName = "刪除自動產碼角色後再新增不回收已刪除代碼（012，設計決策 R5）")]
    public async Task Autogenerated_code_is_not_reused_after_soft_delete()
    {
        var first = await CreateWithoutCodeAsync("回收測試一");
        var firstCode = (await MaintenanceAsync()).Items.Single(i => i.Id == first).Code;

        var delete = await _client.DeleteAsync($"/roles/{first}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var second = await CreateWithoutCodeAsync("回收測試二");
        var secondCode = (await MaintenanceAsync()).Items.Single(i => i.Id == second).Code;

        Assert.NotEqual(firstCode, secondCode);
        Assert.True(int.Parse(secondCode[1..]) > int.Parse(firstCode[1..]));
    }

    private async Task<Guid> CreateWithoutCodeAsync(string name)
    {
        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code = (string?)null, name, description = (string?)null, roleType = "Custom" },
            Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedDto>(Ct))!.Id;
    }
}
