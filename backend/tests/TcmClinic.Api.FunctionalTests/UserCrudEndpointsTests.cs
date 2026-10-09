using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>使用者管理與登入端點的功能測試（US1–US5，對真實 SQL 容器）。</summary>
public class UserCrudEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record RoleItem(Guid Id, string Code, string Name, string? Description);
    private sealed record Created(Guid Id);
    private sealed record UserItem(Guid Id, string Account, string DisplayName, string[] RoleCodes, string[] RoleNames);
    private sealed record UserList(UserItem[] Items, int Page, int PageSize, int TotalCount);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Guid> CreateRoleAsync(string code)
    {
        var response = await _client.PostAsJsonAsync(
            "/roles", new { code, name = code, description = (string?)null, roleType = "Custom" }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>(Ct))!.Id;
    }

    private Task<HttpResponseMessage> CreateUserAsync(
        string account, string displayName, string password, Guid[] roleIds) =>
        _client.PostAsJsonAsync("/users", new { account, displayName, password, roleIds }, Ct);

    private Task<HttpResponseMessage> LoginAsync(string account, string password) =>
        _client.PostAsJsonAsync("/auth/login", new { account, password }, Ct);

    private async Task<UserList> ListAsync(string keyword = "") =>
        (await _client.GetFromJsonAsync<UserList>($"/users?page=1&pageSize=50&keyword={keyword}", Ct))!;

    // 情境：新增使用者 → 出現在清單且可用所設帳密登入（US3，FR-011）。
    // @SPEC-004 @AC-8
    // @SPEC-004 @AC-10
    [Fact(DisplayName = "新增使用者後出現在清單且可用所設帳密登入（US3，FR-011）")]
    public async Task Create_user_appears_in_list_and_can_login()
    {
        var roleId = await CreateRoleAsync("ED_LOGIN");
        var create = await CreateUserAsync("alice", "Alice", "P@ssw0rd1234", [roleId]);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await ListAsync("alice");
        Assert.Contains(list.Items, u => u.Account == "alice");

        var login = await LoginAsync("alice", "P@ssw0rd1234");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers, h => h.Key == "Set-Cookie");
    }

    // 情境：以大小寫不同的重複帳號新增 → 400（帳號不分大小寫唯一，FR-012）。
    // @SPEC-004 @AC-11
    [Fact(DisplayName = "大小寫不同的重複帳號新增回應 400（帳號不分大小寫唯一，FR-012）")]
    public async Task Duplicate_account_case_insensitive_returns_400()
    {
        var roleId = await CreateRoleAsync("ED_DUP");
        await CreateUserAsync("bob", "Bob", "P@ssw0rd1234", [roleId]);

        var dup = await CreateUserAsync("BOB", "Bob2", "P@ssw0rd1234", [roleId]);
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);
    }

    // 情境：弱密碼（未達 12 碼、缺大寫、缺小寫或缺數字）→ 400（FR-013，經 013 FR-008a 改為全系統統一規則）。
    // @SPEC-004 @AC-12
    // @SPEC-013 @AC-22
    [Theory(DisplayName = "弱密碼（未達 12 碼或缺大寫、小寫、數字）新增回應 400（FR-013、013 FR-008a）")]
    [InlineData("WEAK11", "Abcdefghij1")]
    [InlineData("WEAKUP", "abcdefghijk1")]
    [InlineData("WEAKLO", "ABCDEFGHIJK1")]
    [InlineData("WEAKDG", "Abcdefghijkl")]
    public async Task Weak_password_returns_400(string suffix, string weakPassword)
    {
        var roleId = await CreateRoleAsync($"ED_{suffix}");
        var weak = await CreateUserAsync($"carol{suffix.ToLowerInvariant()}", "Carol", weakPassword, [roleId]);
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    // 情境：管理者重設密碼時新密碼未達 12 碼或缺大寫、小寫、數字 → 400（FR-014，013 FR-008a）。
    // @SPEC-004 @AC-21
    // @SPEC-013 @AC-22
    [Theory(DisplayName = "重設密碼時弱密碼回應 400（FR-014、013 FR-008a）")]
    [InlineData("RWK11", "Abcdefghij1")]
    [InlineData("RWKUP", "abcdefghijk1")]
    [InlineData("RWKLO", "ABCDEFGHIJK1")]
    [InlineData("RWKDG", "Abcdefghijkl")]
    public async Task Weak_password_on_reset_returns_400(string suffix, string weakPassword)
    {
        var roleId = await CreateRoleAsync($"ED_{suffix}");
        var created = await (await CreateUserAsync($"kate{suffix.ToLowerInvariant()}", "Kate", "P@ssw0rd1234", [roleId]))
            .Content.ReadFromJsonAsync<Created>(Ct);

        var reset = await _client.PutAsJsonAsync(
            $"/users/{created!.Id}/password", new { newPassword = weakPassword }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
    }

    // 情境：新增時密碼超過 128 碼上限 → 400（FR-013 上限，防過長輸入之資源濫用）。
    // @SPEC-004 @AC-21
    [Fact(DisplayName = "新增時密碼超過 128 碼上限回應 400（FR-013 上限）")]
    public async Task Overlong_password_on_create_returns_400()
    {
        var roleId = await CreateRoleAsync("ED_LONG");
        var overlong = "A1" + new string('a', 127); // 129 碼、含大寫、小寫、數字，僅違反上限（013 FR-008a）
        var res = await CreateUserAsync("ivan", "Ivan", overlong, [roleId]);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // 情境：未指派角色 → 400（每位使用者至少一個角色，FR-011/016）。
    // @SPEC-004 @AC-16
    [Fact(DisplayName = "未指派角色新增回應 400（每位使用者至少一角色，FR-011/016）")]
    public async Task Empty_roles_returns_400()
    {
        var noRole = await CreateUserAsync("dave", "Dave", "P@ssw0rd1234", []);
        Assert.Equal(HttpStatusCode.BadRequest, noRole.StatusCode);
    }

    // 情境：修改使用者名稱與角色 → 200，清單反映新名稱（US4，FR-014）。
    // @SPEC-004 @AC-14
    [Fact(DisplayName = "修改使用者名稱與角色回應 200 且清單反映新名稱（US4，FR-014）")]
    public async Task Update_changes_display_name()
    {
        var roleId = await CreateRoleAsync("ED_UPD");
        var created = await (await CreateUserAsync("erin", "Erin", "P@ssw0rd1234", [roleId]))
            .Content.ReadFromJsonAsync<Created>(Ct);

        var update = await _client.PutAsJsonAsync(
            $"/users/{created!.Id}", new { displayName = "Erin 新", roleIds = new[] { roleId } }, Ct);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var list = await ListAsync("erin");
        Assert.Contains(list.Items, u => u.Id == created.Id && u.DisplayName == "Erin 新");
    }

    // 情境（SPEC-013，FR-011）：管理者新增使用者與重設密碼皆記錄上次修改密碼時間為當下（效期自此起算）。
    // @SPEC-013 @AC-26
    [Fact(DisplayName = "新增使用者與重設密碼皆更新上次修改密碼時間（013，FR-011）")]
    public async Task Create_and_reset_record_password_changed_at()
    {
        var roleId = await CreateRoleAsync("ED_PCA");
        var created = await (await CreateUserAsync("leo", "Leo", "P@ssw0rd1234", [roleId]))
            .Content.ReadFromJsonAsync<Created>(Ct);

        var afterCreate = await ProfileTestSupport.QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .Where(u => u.PublicId == Core.UserAggregate.UserPublicId.From(created!.Id))
            .Select(u => u.PasswordChangedAt).SingleAsync(Ct));
        Assert.NotNull(afterCreate);
        Assert.True(Math.Abs((DateTime.Now - afterCreate.Value).TotalMinutes) < 1);

        // 把修改時間推回 30 天前，驗證重設後確實更新為當下。
        await ProfileTestSupport.WithDbAsync(factory, ctx => ctx.Database.ExecuteSqlRawAsync(
            "UPDATE SysUser SET PasswordChangedAt = {0} WHERE PublicId = {1}",
            new object[] { DateTime.Now.AddDays(-30), created!.Id }, Ct));

        var reset = await _client.PutAsJsonAsync(
            $"/users/{created!.Id}/password", new { newPassword = "N3wP@ssw0rd99" }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var afterReset = await ProfileTestSupport.QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .Where(u => u.PublicId == Core.UserAggregate.UserPublicId.From(created.Id))
            .Select(u => u.PasswordChangedAt).SingleAsync(Ct));
        Assert.True(Math.Abs((DateTime.Now - afterReset!.Value).TotalMinutes) < 1);
    }

    // 情境：重設密碼後舊密碼失效、僅新密碼可登入（US4，FR-014）。
    // @SPEC-004 @AC-15
    [Fact(DisplayName = "重設密碼後舊密碼失效且僅新密碼可登入（US4，FR-014）")]
    public async Task Reset_password_invalidates_old()
    {
        var roleId = await CreateRoleAsync("ED_PWD");
        var created = await (await CreateUserAsync("frank", "Frank", "P@ssw0rd1234", [roleId]))
            .Content.ReadFromJsonAsync<Created>(Ct);

        var reset = await _client.PutAsJsonAsync(
            $"/users/{created!.Id}/password", new { newPassword = "N3wP@ssw0rd99" }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("frank", "P@ssw0rd1234")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("frank", "N3wP@ssw0rd99")).StatusCode);
    }

    // 情境：重設密碼時新密碼超過 128 碼上限 → 400（FR-013 上限，經 FR-014 套用於重設）。
    // @SPEC-004 @AC-21
    [Fact(DisplayName = "重設密碼時新密碼超過 128 碼上限回應 400（FR-013 上限，FR-014）")]
    public async Task Overlong_password_on_reset_returns_400()
    {
        var roleId = await CreateRoleAsync("ED_RLONG");
        var created = await (await CreateUserAsync("judy", "Judy", "P@ssw0rd1234", [roleId]))
            .Content.ReadFromJsonAsync<Created>(Ct);

        var overlong = "A1" + new string('a', 127); // 129 碼、含大寫、小寫、數字，僅違反上限（013 FR-008a）
        var reset = await _client.PutAsJsonAsync(
            $"/users/{created!.Id}/password", new { newPassword = overlong }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
    }

    // 情境：軟刪除使用者 → 自清單消失，且相同帳號可重用（US5 情境 3，FR-017）。
    // @SPEC-004 @AC-7
    // @SPEC-004 @AC-17
    // @SPEC-004 @AC-19
    [Fact(DisplayName = "軟刪除使用者後自清單消失且相同帳號可重用（US5，FR-017）")]
    public async Task Delete_soft_removes_and_allows_account_reuse()
    {
        var roleId = await CreateRoleAsync("ED_DEL");
        var created = await (await CreateUserAsync("grace", "Grace", "P@ssw0rd1234", [roleId]))
            .Content.ReadFromJsonAsync<Created>(Ct);

        var delete = await _client.DeleteAsync($"/users/{created!.Id}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await ListAsync("grace");
        Assert.DoesNotContain(list.Items, u => u.Id == created.Id);

        var reuse = await CreateUserAsync("grace", "Grace 2", "P@ssw0rd1234", [roleId]);
        Assert.Equal(HttpStatusCode.Created, reuse.StatusCode);
    }

    // 情境（005）：無防鎖死 → 刪除唯一管理帳號（種子 admin）亦成功回 204（US3、SC-004）。
    // @SPEC-004 @AC-18
    // @SPEC-005 @AC-7
    [Fact(DisplayName = "刪除唯一超級管理員應成功回 204（005 移除防鎖死）")]
    public async Task Delete_last_superadmin_succeeds_no_antilockout()
    {
        var users = await ListAsync("admin");
        var admin = users.Items.Single(u => u.Account == "admin");

        var delete = await _client.DeleteAsync($"/users/{admin.Id}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    // 情境：清單回應不得洩漏密碼、雜湊或 salt（FR-009）。
    // @SPEC-004 @AC-9
    [Fact(DisplayName = "清單回應不得洩漏密碼、雜湊或 salt（FR-009）")]
    public async Task List_does_not_expose_password()
    {
        var raw = await _client.GetStringAsync("/users?page=1&pageSize=50", Ct);
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordSalt", raw, StringComparison.OrdinalIgnoreCase);
    }

    // 情境：連續 5 次登入失敗後，第 6 次即使密碼正確亦被鎖定回 401（FR-006a、SC-008）。
    // @SPEC-004 @AC-20
    [Fact(DisplayName = "連續 5 次登入失敗後即使密碼正確亦鎖定回 401（FR-006a、SC-008）")]
    public async Task Locks_account_after_five_failures()
    {
        var roleId = await CreateRoleAsync("ED_LOCK");
        await CreateUserAsync("henry", "Henry", "P@ssw0rd1234", [roleId]);

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("henry", "wrong")).StatusCode);
        }

        // 鎖定期間即使正確密碼亦拒絕。
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("henry", "P@ssw0rd1234")).StatusCode);
    }
}
