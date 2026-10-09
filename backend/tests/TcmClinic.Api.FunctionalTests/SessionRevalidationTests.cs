using System.Net;
using System.Net.Http.Json;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>工作階段每請求重驗與角色刷新（R8，FR-015 + 刪除邊界；真實 Cookie scheme）。</summary>
public class SessionRevalidationTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record Created(Guid Id);
    private sealed record Me(string Account, string DisplayName, string[] Roles);

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = factory.CreateClient();
        var login = await AuthTestSupport.PostLoginAsync(client, "admin", "Admin123!", Ct);
        login.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<Guid> CreateRoleAsync(HttpClient admin, string code)
    {
        var res = await admin.PostAsJsonAsync(
            "/roles", new { code, name = code, description = (string?)null, roleType = "Custom" }, Ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<Created>(Ct))!.Id;
    }

    private async Task<Guid> CreateUserAsync(HttpClient admin, string account, Guid[] roleIds)
    {
        var res = await admin.PostAsJsonAsync(
            "/users", new { account, displayName = account, password = "P@ssw0rd1234", roleIds }, Ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<Created>(Ct))!.Id;
    }

    private async Task<HttpClient> LoginAsAsync(string account)
    {
        var client = factory.CreateClient();
        var login = await AuthTestSupport.PostLoginAsync(client, account, "P@ssw0rd1234", Ct);
        login.EnsureSuccessStatusCode();
        return client;
    }

    // 情境：登入中的使用者被超管刪除 → 其下次請求即失去存取（401）（R8 刪除邊界）。
    // @SPEC-004 @AC-6
    // @SPEC-004 @AC-17
    [Fact(DisplayName = "登入中使用者被超管刪除後下次請求即失去存取回應 401（R8 刪除邊界）")]
    public async Task Deleted_user_loses_access_next_request()
    {
        var admin = await AdminClientAsync();
        var roleId = await CreateRoleAsync(admin, "REVED");
        var userId = await CreateUserAsync(admin, "xavier", [roleId]);

        var userClient = await LoginAsAsync("xavier");
        Assert.Equal(HttpStatusCode.OK, (await userClient.GetAsync("/auth/me", Ct)).StatusCode);

        var delete = await admin.DeleteAsync($"/users/{userId}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await userClient.GetAsync("/auth/me", Ct)).StatusCode);
    }

    // 情境：登入中的使用者角色被變更 → 其下次請求即反映新角色（R8，FR-015）。
    // @SPEC-004 @AC-14
    [Fact(DisplayName = "登入中使用者角色被變更後下次請求即反映新角色（R8，FR-015）")]
    public async Task Role_change_reflected_next_request()
    {
        var admin = await AdminClientAsync();
        var roleA = await CreateRoleAsync(admin, "REVA");
        var roleB = await CreateRoleAsync(admin, "REVB");
        var userId = await CreateUserAsync(admin, "yancy", [roleA]);

        var userClient = await LoginAsAsync("yancy");
        var me1 = await userClient.GetFromJsonAsync<Me>("/auth/me", Ct);
        Assert.Contains("REVA", me1!.Roles);

        var update = await admin.PutAsJsonAsync(
            $"/users/{userId}", new { displayName = "yancy", roleIds = new[] { roleB } }, Ct);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var me2 = await userClient.GetFromJsonAsync<Me>("/auth/me", Ct);
        Assert.Contains("REVB", me2!.Roles);
        Assert.DoesNotContain("REVA", me2.Roles);
    }
}
