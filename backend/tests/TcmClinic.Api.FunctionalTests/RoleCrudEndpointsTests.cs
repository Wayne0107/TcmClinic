using System.Net;
using System.Net.Http.Json;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>角色 CRUD 端點的功能測試（US4，對真實 SQL 容器）。</summary>
public class RoleCrudEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public ValueTask InitializeAsync() => new(RoleTestSupport.EnsureSampleRolesAsync(factory));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed record RoleDto(Guid Id, string Code, string Name, string? Description);
    private sealed record CreatedDto(Guid Id);

    private async Task<RoleDto[]> RolesAsync() =>
        (await _client.GetFromJsonAsync<RoleDto[]>("/roles", TestContext.Current.CancellationToken))!;

    private async Task<Guid> CreateRoleAsync(string code, string name, string roleType = "Custom")
    {
        var response = await _client.PostAsJsonAsync(
            "/roles",
            new { code, name, description = (string?)null, roleType },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedDto>(TestContext.Current.CancellationToken);
        return created!.Id;
    }

    // 情境：建立角色 → 隨後查詢清單能看到該角色（FR-013 建立角色，端到端）。
    // @SPEC-002 @AC-11
    // @SPEC-012 @AC-5
    [Fact(DisplayName = "建立角色後查詢清單應看到該角色（FR-013）")]
    public async Task Create_role_then_appears_in_list()
    {
        await CreateRoleAsync("CS", "客服");
        var roles = await RolesAsync();
        Assert.Contains(roles, r => r.Code == "CS" && r.Name == "客服");
    }

    // 情境：以已存在的角色代碼再建立 → 回 400 BadRequest（角色代碼唯一）。
    // @SPEC-002 @AC-11
    // @SPEC-012 @AC-6
    [Fact(DisplayName = "以重複角色代碼建立應回 400（角色代碼唯一）")]
    public async Task Duplicate_code_returns_400()
    {
        await CreateRoleAsync("DUP", "第一個");
        var second = await _client.PostAsJsonAsync(
            "/roles",
            new { code = "DUP", name = "第二個", description = (string?)null, roleType = "Custom" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    // 情境：更新角色名稱 → 回 200 OK，且清單反映新名稱（FR-013 編輯角色）。
    // @SPEC-002 @AC-11
    [Fact(DisplayName = "更新角色名稱應回 200 且清單反映新名稱（FR-013）")]
    public async Task Update_role_changes_name()
    {
        var id = await CreateRoleAsync("UPD", "舊名");

        var update = await _client.PutAsJsonAsync(
            $"/roles/{id}",
            new { name = "新名", description = "說明" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var roles = await RolesAsync();
        Assert.Contains(roles, r => r.Id == id && r.Name == "新名");
    }

    // 情境：刪除一般角色 → 回 204 NoContent，且清單不再包含它（軟刪除後查不到，FR-013）。
    // @SPEC-002 @AC-11
    [Fact(DisplayName = "刪除一般角色應回 204 且清單不再包含（FR-013）")]
    public async Task Delete_role_removes_it()
    {
        var id = await CreateRoleAsync("DEL", "待刪");

        var delete = await _client.DeleteAsync($"/roles/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var roles = await RolesAsync();
        Assert.DoesNotContain(roles, r => r.Id == id);
    }

    // 情境（012 取代 005）：SUPERADMIN 種為 Custom（非 System，無超管特例），但初始 admin 使用者已指派它，
    // 故受 FR-012「已指派不可刪」保護 → 回 400（伺服器端強制，SC-003）。
    // @SPEC-002 @AC-12
    // @SPEC-012 @AC-13
    [Fact(DisplayName = "刪除已被指派的 SUPERADMIN 角色應回 400（012 FR-012 取代 005）")]
    public async Task Delete_assigned_superadmin_returns_400()
    {
        var roles = await RolesAsync();
        var superAdmin = roles.Single(r => r.Code == "SUPERADMIN");

        var delete = await _client.DeleteAsync($"/roles/{superAdmin.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    // 情境（012，US4）：System 類角色不可刪除 → 回 400（FR-013，伺服器端強制）。
    // @SPEC-012 @AC-14
    [Fact(DisplayName = "刪除 System 類角色應回 400（012，FR-013）")]
    public async Task Delete_system_role_returns_400()
    {
        var roles = await RolesAsync();
        var sm = roles.Single(r => r.Code == RoleTestSupport.SystemRoleCode);

        var delete = await _client.DeleteAsync($"/roles/{sm.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    // 情境（012，US4）：未指派的一般（Custom）角色可刪除 → 回 204（FR-011）。
    // @SPEC-012 @AC-12
    [Fact(DisplayName = "刪除未指派的一般角色應回 204（012，FR-011）")]
    public async Task Delete_unassigned_custom_role_succeeds()
    {
        var id = await CreateRoleAsync("DEL012", "待刪自訂", "Custom");

        var delete = await _client.DeleteAsync($"/roles/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }
}
