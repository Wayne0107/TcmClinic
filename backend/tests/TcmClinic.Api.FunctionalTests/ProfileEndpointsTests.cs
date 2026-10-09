using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.SharedKernel;
using static TcmClinic.Api.FunctionalTests.ProfileTestSupport;
using static TcmClinic.Api.Tests.Shared.ProblemDetailsTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 個人資料維護（SPEC-013）檢視與修改基本資料的功能測試（US1、US2；真實 Cookie 登入）。需 Docker Desktop。
/// </summary>
public class ProfileEndpointsTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly string[] ExpectedFields =
    [
        "account", "displayName", "roles", "phone", "email",
        "passwordChangedAt", "passwordRemainingDays", "passwordExpired", "status",
    ];

    // ── US1：檢視 ─────────────────────────────────────────────────────────────

    // 情境（US1/AC1、AC3）：登入者查詢個人資料，回傳本人資料；未填的電話與 email 為 null（FR-001、FR-013）。
    // @SPEC-013 @AC-1
    // @SPEC-013 @AC-3
    // @SPEC-013 @AC-17
    [Fact(DisplayName = "查詢個人資料回傳登入者本人資料（013，US1/AC1、FR-001）")]
    public async Task Get_returns_own_profile()
    {
        await CreateUserAsync(factory, "p1view", displayName: "檢視者");
        var client = await LoginAsync(factory, "p1view");

        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);

        Assert.NotNull(profile);
        Assert.Equal("p1view", profile.Account);
        Assert.Equal("檢視者", profile.DisplayName);
        Assert.Contains(profile.Roles, r => r.Code == DefaultRoleCode && r.Name == DefaultRoleName);
        Assert.Null(profile.Phone);
        Assert.Null(profile.Email);
        Assert.Equal(Today, profile.PasswordChangedAt);
        Assert.Equal(90, profile.PasswordRemainingDays);
        Assert.False(profile.PasswordExpired);
        Assert.Equal("Enabled", profile.Status);
    }

    // 情境（US1/AC2）：回應欄位恰為契約所列，不含任何密碼、雜湊或 salt（FR-001、SC-006）。
    // @SPEC-013 @AC-2
    [Fact(DisplayName = "查詢個人資料不含密碼、雜湊或 salt（013，US1/AC2、SC-006）")]
    public async Task Get_does_not_expose_credentials()
    {
        await CreateUserAsync(factory, "p1secret");
        var client = await LoginAsync(factory, "p1secret");

        var raw = await client.GetStringAsync("/profile", Ct);

        using var doc = JsonDocument.Parse(raw);
        var names = doc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.Equal(ExpectedFields.ToHashSet(), names);

        var (hash, salt) = await QueryDbAsync(factory, async ctx =>
        {
            var u = await ctx.Users.AsNoTracking().SingleAsync(x => x.Account == UserAccount.From("p1secret"), Ct);
            return (u.PasswordHash, u.PasswordSalt);
        });
        Assert.DoesNotContain(hash, raw);
        Assert.DoesNotContain(salt, raw);
        Assert.DoesNotContain(Password, raw);
        Assert.DoesNotContain("hash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("salt", raw, StringComparison.OrdinalIgnoreCase);
    }

    // 情境（US1/AC4）：未登入者查詢個人資料 → 401。
    // @SPEC-013 @AC-4
    [Fact(DisplayName = "未登入查詢個人資料回應 401（013，US1/AC4）")]
    public async Task Get_requires_login()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/profile", Ct)).StatusCode);
    }

    // 情境：角色不具 personalData 選單可見性的使用者 → 403（FR-014）。
    // @SPEC-013 @AC-4
    [Fact(DisplayName = "無個人資料維護選單權限者查詢回應 403（013，FR-014）")]
    public async Task Get_requires_personal_data_menu()
    {
        await CreateRoleAsync(factory, "P1NOMENU");
        await CreateUserAsync(factory, "p1nomenu", roleCode: "P1NOMENU");
        var client = await LoginAsync(factory, "p1nomenu");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/profile", Ct)).StatusCode);
    }

    // 情境：兩名使用者各自登入，只會看到自己的資料（FR-002、SC-002）。
    // @SPEC-013 @AC-1
    // @SPEC-013 @AC-8
    [Fact(DisplayName = "兩名使用者各自只看到自己的個人資料（013，FR-002）")]
    public async Task Each_user_sees_only_own_profile()
    {
        await CreateUserAsync(factory, "p1alice", displayName: "Alice");
        await CreateUserAsync(factory, "p1bob", displayName: "Bob");

        var alice = await (await LoginAsync(factory, "p1alice")).GetFromJsonAsync<Profile>("/profile", Ct);
        var bob = await (await LoginAsync(factory, "p1bob")).GetFromJsonAsync<Profile>("/profile", Ct);

        Assert.Equal("p1alice", alice!.Account);
        Assert.Equal("Alice", alice.DisplayName);
        Assert.Equal("p1bob", bob!.Account);
        Assert.Equal("Bob", bob.DisplayName);
    }

    // ── US2：修改基本資料 ──────────────────────────────────────────────────────

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, object body) =>
        client.PutAsJsonAsync("/profile", body, Ct);

    private Task<List<Core.Auditing.UserLog>> UserLogsAsync(string account) =>
        QueryDbAsync(factory, ctx => ctx.UserLogs.AsNoTracking()
            .Where(l => l.CreateUser == account).OrderBy(l => l.Id).ToListAsync(Ct));

    // 情境（US2/AC1）：修改名稱、電話、email（不改密碼）→ 200 且訊息對齊舊系統；值去除前後空白；原密碼仍可登入（FR-003、FR-016）。
    // @SPEC-013 @AC-5
    // @SPEC-013 @AC-16
    [Fact(DisplayName = "修改基本資料成功並回傳舊系統格式訊息，原密碼仍可登入（013，US2/AC1）")]
    public async Task Put_updates_basic_data()
    {
        await CreateUserAsync(factory, "p2basic");
        var client = await LoginAsync(factory, "p2basic");

        var res = await PutAsync(client, new
        {
            displayName = "  新名稱  ", phone = " 02-2345-6789#123 ", email = " p2@example.com ",
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<UpdateResult>(Ct);
        Assert.Equal("個人資料維護 (p2basic) [新名稱] 修改成功", body!.Message);

        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);
        Assert.Equal("新名稱", profile!.DisplayName);
        Assert.Equal("02-2345-6789#123", profile.Phone);
        Assert.Equal("p2@example.com", profile.Email);

        await LoginAsync(factory, "p2basic"); // 密碼未變更，原密碼仍可登入
    }

    // 情境（Edge Case）：電話與 email 送空字串或 null → 清除為 null。
    // @SPEC-013 @AC-29
    [Fact(DisplayName = "電話與電子郵件送空值即清除（013，Edge Case）")]
    public async Task Put_clears_contact_with_empty_values()
    {
        await CreateUserAsync(factory, "p2clear");
        var client = await LoginAsync(factory, "p2clear");
        await PutAsync(client, new { displayName = "p2clear", phone = "0912345678", email = "a@b.co" });

        var res = await PutAsync(client, new { displayName = "p2clear", phone = "", email = (string?)null });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);
        Assert.Null(profile!.Phone);
        Assert.Null(profile.Email);
    }

    // 情境（US2/AC4、SC-002）：請求夾帶他人的 id 與帳號 → 只修改登入者本人，他人資料不變（FR-002）。
    // @SPEC-013 @AC-8
    [Fact(DisplayName = "請求夾帶他人識別碼仍只修改本人（013，US2/AC4、FR-002）")]
    public async Task Put_ignores_foreign_identifiers()
    {
        await CreateUserAsync(factory, "p2self", displayName: "本人");
        var victimId = await CreateUserAsync(factory, "p2victim", displayName: "他人");
        var client = await LoginAsync(factory, "p2self");

        var res = await PutAsync(client, new
        {
            id = victimId, account = "p2victim", displayName = "竄改", phone = "000", email = "x@y.zz",
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var victim = await QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .SingleAsync(u => u.Account == UserAccount.From("p2victim"), Ct));
        Assert.Equal("他人", victim.DisplayName.Value);
        Assert.Null(victim.Phone);

        var self = await client.GetFromJsonAsync<Profile>("/profile", Ct);
        Assert.Equal("竄改", self!.DisplayName);
    }

    // 情境（US2/AC2、AC3、Edge Case）：名稱空白或過長、email 或電話格式錯誤 → 400 並指出欄位（FR-004～FR-007）。
    // @SPEC-013 @AC-6
    // @SPEC-013 @AC-7
    // @SPEC-013 @AC-24
    [Theory(DisplayName = "基本資料格式錯誤回應 400 並指出欄位（013，US2/AC2、AC3）")]
    [InlineData("   ", null, null, "displayName")]
    [InlineData("LONG101", null, null, "displayName")]
    [InlineData("ok", null, "abc", "email")]
    [InlineData("ok", "09xx-abc", null, "phone")]
    [InlineData("ok", "012345678901234567890", null, "phone")]
    public async Task Put_rejects_invalid_basic_data(string displayName, string? phone, string? email, string field)
    {
        var account = $"p2bad{Guid.NewGuid():N}"[..20];
        await CreateUserAsync(factory, account);
        var client = await LoginAsync(factory, account);
        var name = displayName == "LONG101" ? new string('名', 101) : displayName;

        var res = await PutAsync(client, new { displayName = name, phone, email });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True((await ErrorsAsync(res)).ContainsKey(field));
        Assert.Empty(await UserLogsAsync(account)); // 格式驗證即拒絕的請求不記操作紀錄（FR-015）
    }

    // 情境（FR-007、Edge Case）：名稱合法但 email 不合法 → 整筆拒絕，名稱也不得被修改（無部分成功）。
    // @SPEC-013 @AC-21
    [Fact(DisplayName = "任一欄位不合法即整筆拒絕，名稱亦不修改（013，FR-007）")]
    public async Task Put_is_atomic()
    {
        await CreateUserAsync(factory, "p2atomic", displayName: "原名");
        var client = await LoginAsync(factory, "p2atomic");

        var res = await PutAsync(client, new { displayName = "新名", email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);
        Assert.Equal("原名", profile!.DisplayName);
    }

    // 情境（FR-015）：修改成功後留下一筆成功操作紀錄；異動紀錄保留電話與 email 的改前改後值（設計決策 R6）。
    // @SPEC-013 @AC-24
    [Fact(DisplayName = "修改成功寫入操作紀錄與含聯絡資料的異動紀錄（013，FR-015）")]
    public async Task Put_writes_user_log_and_audit_log()
    {
        var id = await CreateUserAsync(factory, "p2log");
        var client = await LoginAsync(factory, "p2log");

        var res = await PutAsync(client, new { displayName = "紀錄者", phone = "0911222333", email = "log@example.com" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var log = Assert.Single(await UserLogsAsync("p2log"));
        Assert.Equal("PersonalData", log.FunctionId);
        Assert.Equal("Modify", log.Action);
        Assert.Equal("Success", log.Status);
        Assert.Equal("個人資料維護 (p2log) [紀錄者] 修改成功", log.Content);

        var pk = id.ToString();
        var audit = await QueryDbAsync(factory, ctx => ctx.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == "User" && a.PrimaryKey == pk && a.Operation == "Update")
            .ToListAsync(Ct));
        Assert.Contains(audit, a => a.NewValues!.Contains("0911222333") && a.NewValues.Contains("log@example.com"));
    }

    // 情境（設計決策 R12）：改名後 GET /auth/me 的顯示名稱立即為新名稱（每請求刷新）。
    // @SPEC-013 @AC-31
    [Fact(DisplayName = "改名後 /auth/me 的顯示名稱立即更新（013，設計決策 R12）")]
    public async Task Rename_is_reflected_in_auth_me()
    {
        await CreateUserAsync(factory, "p2me", displayName: "舊名");
        var client = await LoginAsync(factory, "p2me");

        await PutAsync(client, new { displayName = "新名" });

        using var me = JsonDocument.Parse(await client.GetStringAsync("/auth/me", Ct));
        Assert.Equal("新名", me.RootElement.GetProperty("displayName").GetString());
    }

    // 情境（Edge Case）：只填目前密碼、新密碼留空 → 視為不變更密碼，目前密碼忽略、不驗證。
    // @SPEC-013 @AC-16
    [Fact(DisplayName = "只填目前密碼而未填新密碼時忽略目前密碼（013，Edge Case）")]
    public async Task Current_password_is_ignored_without_new_password()
    {
        await CreateUserAsync(factory, "p2ignore");
        var client = await LoginAsync(factory, "p2ignore");

        var res = await PutAsync(client, new { displayName = "p2ignore", currentPassword = "亂填的密碼" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        await LoginAsync(factory, "p2ignore"); // 密碼未變更
    }

    /// <summary>模擬儲存失敗的使用者 Repository：UpdateAsync 一律拋出例外。</summary>
    private sealed class FailingUserRepository(AppDbContext dbContext) : EfRepository<User>(dbContext)
    {
        public override Task<int> UpdateAsync(User entity, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("模擬資料庫暫時無法使用");
    }

    // 情境（spec Edge Case：非預期錯誤、FR-016、contracts §2）：儲存時發生非預期錯誤 → 500 ProblemDetails，
    // detail 為「個人資料維護 (帳號) [名稱] 修改失敗」且不外洩例外細節；資料未變、留下一筆失敗操作紀錄。
    // @SPEC-013 @AC-27
    [Fact(DisplayName = "儲存發生非預期錯誤時回應 500 與修改失敗訊息（013，FR-016）")]
    public async Task Unexpected_save_error_returns_problem_details()
    {
        await CreateUserAsync(factory, "p2boom", displayName: "原名");
        using var failing = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<IRepository<User>, FailingUserRepository>()));
        var client = await LoginAsync(failing, "p2boom");

        var res = await PutAsync(client, new { displayName = "新名" });

        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
        var raw = await res.Content.ReadAsStringAsync(Ct);
        using var problem = JsonDocument.Parse(raw);
        Assert.Equal("個人資料維護 (p2boom) [新名] 修改失敗", problem.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("模擬資料庫暫時無法使用", raw);

        var name = await QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .Where(u => u.Account == UserAccount.From("p2boom")).Select(u => u.DisplayName).SingleAsync(Ct));
        Assert.Equal("原名", name.Value);
        var log = Assert.Single(await UserLogsAsync("p2boom"));
        Assert.Equal("Failure", log.Status);
    }

    // 情境（Edge Case）：本人於登入中被軟刪除 → 再修改回應 401 且資料不變（每請求重驗）。
    // @SPEC-013 @AC-29
    [Fact(DisplayName = "已刪除者無法修改個人資料（013，Edge Case）")]
    public async Task Deleted_user_cannot_update()
    {
        await CreateUserAsync(factory, "p2deleted", displayName: "刪除前");
        var client = await LoginAsync(factory, "p2deleted");

        await WithDbAsync(factory, async ctx =>
        {
            var user = await ctx.Users.SingleAsync(u => u.Account == UserAccount.From("p2deleted"), Ct);
            ctx.Users.Remove(user);
            await ctx.SaveChangesAsync(Ct);
        });

        var res = await PutAsync(client, new { displayName = "刪除後" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var name = await QueryDbAsync(factory, ctx => ctx.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Account == UserAccount.From("p2deleted")).Select(u => u.DisplayName).SingleAsync(Ct));
        Assert.Equal("刪除前", name.Value);
    }
}
