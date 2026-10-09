using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.UserAggregate;
using static TcmClinic.Api.FunctionalTests.ProfileTestSupport;
using static TcmClinic.Api.Tests.Shared.ProblemDetailsTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 個人資料維護（SPEC-013）變更本人密碼的功能測試（US3，FR-008～FR-010、SC-003、SC-003a、SC-004）。
/// 登入鎖定計數存於行程記憶體、同一工廠內共用，故每個案例皆使用專屬帳號。需 Docker Desktop。
/// </summary>
public class ProfilePasswordEndpointsTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string NewPassword = "N3wPassword99";

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, object body) =>
        client.PutAsJsonAsync("/profile", body, Ct);

    private Task<string> HashOfAsync(string account) =>
        QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .Where(u => u.Account == UserAccount.From(account)).Select(u => u.PasswordHash).SingleAsync(Ct));

    private Task<List<string>> LogStatusesAsync(string account) =>
        QueryDbAsync(factory, ctx => ctx.UserLogs.AsNoTracking()
            .Where(l => l.CreateUser == account).OrderBy(l => l.Id).Select(l => l.Status).ToListAsync(Ct));

    private async Task<HttpStatusCode> LoginStatusAsync(string account, string password)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/auth/login", new { account, password }, Ct);
        return res.StatusCode;
    }

    // 情境（US3/AC1、SC-004）：目前密碼正確且新密碼合法一致 → 200；舊密碼失效、新密碼可登入；
    // 上次修改密碼日為今日、剩餘天數重置為 90；記一筆成功操作紀錄（FR-010）。
    // @SPEC-013 @AC-9
    [Fact(DisplayName = "變更密碼成功後舊密碼失效、新密碼可登入且效期重置（013，US3/AC1、SC-004）")]
    public async Task Change_password_succeeds()
    {
        await CreateUserAsync(factory, "p3ok");
        await WithDbAsync(factory, async ctx =>
        {
            // 先把修改時間推回 30 天前，以驗證變更後確實重新起算。
            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE SysUser SET PasswordChangedAt = {0} WHERE Account = {1}",
                new object[] { DateTime.Now.AddDays(-30), "p3ok" }, Ct);
        });
        var client = await LoginAsync(factory, "p3ok");

        var res = await PutAsync(client, new
        {
            displayName = "p3ok", currentPassword = Password, newPassword = NewPassword, confirmNewPassword = NewPassword,
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginStatusAsync("p3ok", Password));
        Assert.Equal(HttpStatusCode.OK, await LoginStatusAsync("p3ok", NewPassword));

        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);
        Assert.Equal(Today, profile!.PasswordChangedAt);
        Assert.Equal(90, profile.PasswordRemainingDays);
        Assert.Equal(new[] { "Success" }, await LogStatusesAsync("p3ok"));
    }

    // 情境（SC-006，analyze U1）：經 PUT /profile 變更密碼後，該次 SysAuditLog 中 SysUser 的密碼雜湊與 salt
    // 改前改後值皆為遮罩 ***，且 OldValues／NewValues 不含資料庫中的新舊雜湊與 salt（沿用「資料表異動紀錄」敏感欄位遮罩）。
    // @SPEC-013 @AC-23
    [Fact(DisplayName = "變更密碼的資料異動紀錄遮罩密碼雜湊與 salt（013，SC-006）")]
    public async Task Change_password_audit_log_masks_credentials()
    {
        var id = await CreateUserAsync(factory, "p3audit");
        var (oldHash, oldSalt) = await CredentialsOfAsync("p3audit");
        var client = await LoginAsync(factory, "p3audit");

        var res = await PutAsync(client, new
        {
            displayName = "p3audit", currentPassword = Password, newPassword = NewPassword, confirmNewPassword = NewPassword,
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var (newHash, newSalt) = await CredentialsOfAsync("p3audit");
        Assert.NotEqual(oldHash, newHash);

        var pk = id.ToString();
        var audit = await QueryDbAsync(factory, ctx => ctx.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == "User" && a.PrimaryKey == pk && a.Operation == "Update")
            .OrderByDescending(a => a.Id).FirstAsync(Ct));

        foreach (var json in new[] { audit.OldValues, audit.NewValues })
        {
            Assert.NotNull(json);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal("***", doc.RootElement.GetProperty(nameof(User.PasswordHash)).GetString());
            Assert.Equal("***", doc.RootElement.GetProperty(nameof(User.PasswordSalt)).GetString());
            foreach (var secret in new[] { oldHash, oldSalt, newHash, newSalt, Password, NewPassword })
            {
                Assert.DoesNotContain(secret, json);
            }
        }
    }

    private Task<(string Hash, string Salt)> CredentialsOfAsync(string account) =>
        QueryDbAsync(factory, async ctx =>
        {
            var u = await ctx.Users.AsNoTracking().SingleAsync(x => x.Account == UserAccount.From(account), Ct);
            return (u.PasswordHash, u.PasswordSalt);
        });

    // 情境（US3/AC2～AC6、SC-003）：各種不合規的密碼欄位 → 400 並指出欄位，密碼雜湊不變。
    // @SPEC-013 @AC-10
    // @SPEC-013 @AC-11
    // @SPEC-013 @AC-12
    // @SPEC-013 @AC-14
    [Theory(DisplayName = "密碼欄位不合規回應 400 並指出欄位，密碼不變（013，US3、SC-003）")]
    [InlineData("Abcdefghij1", "Abcdefghij1", true, "newPassword")]          // 11 碼
    [InlineData("abcdefghijk1", "abcdefghijk1", true, "newPassword")]        // 缺大寫
    [InlineData("ABCDEFGHIJK1", "ABCDEFGHIJK1", true, "newPassword")]        // 缺小寫
    [InlineData("Abcdefghijkl", "Abcdefghijkl", true, "newPassword")]        // 缺數字
    [InlineData("                    ", "                    ", true, "newPassword")] // 全空白：不得視為未變更
    [InlineData(NewPassword, "N3wPassword98", true, "confirmNewPassword")]   // 兩次不一致
    [InlineData(NewPassword, null, true, "confirmNewPassword")]              // 只填新密碼
    [InlineData(null, NewPassword, true, "newPassword")]                     // 只填確認
    [InlineData(NewPassword, NewPassword, false, "currentPassword")]         // 未填目前密碼
    public async Task Invalid_password_fields_are_rejected(
        string? newPassword, string? confirm, bool withCurrent, string field)
    {
        var account = $"p3bad{Guid.NewGuid():N}"[..20];
        await CreateUserAsync(factory, account);
        var before = await HashOfAsync(account);
        var client = await LoginAsync(factory, account);

        var res = await PutAsync(client, new
        {
            displayName = account,
            currentPassword = withCurrent ? Password : null,
            newPassword,
            confirmNewPassword = confirm,
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True((await ErrorsAsync(res)).ContainsKey(field));
        Assert.Equal(before, await HashOfAsync(account));
    }

    // 情境（US3/AC7）：目前密碼錯誤 → 400（currentPassword），名稱亦不修改，並記一筆失敗操作紀錄（FR-009、FR-015）。
    // @SPEC-013 @AC-15
    // @SPEC-013 @AC-21
    [Fact(DisplayName = "目前密碼錯誤時整筆拒絕並記失敗紀錄（013，US3/AC7）")]
    public async Task Wrong_current_password_is_rejected()
    {
        await CreateUserAsync(factory, "p3wrong", displayName: "原名");
        var before = await HashOfAsync("p3wrong");
        var client = await LoginAsync(factory, "p3wrong");

        var res = await PutAsync(client, new
        {
            displayName = "新名", currentPassword = "WrongPassw0rd1", newPassword = NewPassword, confirmNewPassword = NewPassword,
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("目前密碼不正確。", (await ErrorsAsync(res))["currentPassword"]);
        Assert.Equal(before, await HashOfAsync("p3wrong"));
        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);
        Assert.Equal("原名", profile!.DisplayName);
        Assert.Equal(new[] { "Failure" }, await LogStatusesAsync("p3wrong"));
    }

    // 情境（US3/AC5）：新密碼與目前密碼相同 → 400（newPassword「密碼不可與上次相同。」）。
    // @SPEC-013 @AC-13
    [Fact(DisplayName = "新密碼與目前密碼相同時拒絕（013，US3/AC5）")]
    public async Task Same_as_current_password_is_rejected()
    {
        await CreateUserAsync(factory, "p3same");
        var client = await LoginAsync(factory, "p3same");

        var res = await PutAsync(client, new
        {
            displayName = "p3same", currentPassword = Password, newPassword = Password, confirmNewPassword = Password,
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("密碼不可與上次相同。", (await ErrorsAsync(res))["newPassword"]);
    }

    // 情境（設計決策 R4）：連續 5 次目前密碼錯誤 → 第 6 次即使正確也被拒；同帳號登入亦被拒（共用鎖定）。
    // @SPEC-013 @AC-28
    [Fact(DisplayName = "目前密碼連錯 5 次後鎖定，改密碼與登入皆被拒（013，設計決策 R4）")]
    public async Task Repeated_wrong_current_password_locks_account()
    {
        await CreateUserAsync(factory, "p3lock");
        var client = await LoginAsync(factory, "p3lock");
        var wrong = new
        {
            displayName = "p3lock", currentPassword = "WrongPassw0rd1", newPassword = NewPassword, confirmNewPassword = NewPassword,
        };

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await PutAsync(client, wrong)).StatusCode);
        }

        var sixth = await PutAsync(client, new
        {
            displayName = "p3lock", currentPassword = Password, newPassword = NewPassword, confirmNewPassword = NewPassword,
        });

        Assert.Equal(HttpStatusCode.BadRequest, sixth.StatusCode);
        Assert.Contains("嘗試次數過多，請稍後再試。", (await ErrorsAsync(sixth))["currentPassword"]);
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginStatusAsync("p3lock", Password));
    }

    // 情境（設計決策 R4）：錯誤數次後成功變更 → 失敗計數歸零（之後再錯 4 次仍未鎖定）。
    // @SPEC-013 @AC-28
    [Fact(DisplayName = "變更密碼成功後失敗計數歸零（013，設計決策 R4）")]
    public async Task Successful_change_resets_failure_count()
    {
        await CreateUserAsync(factory, "p3reset");
        var client = await LoginAsync(factory, "p3reset");
        object Wrong() => new
        {
            displayName = "p3reset", currentPassword = "WrongPassw0rd1", newPassword = NewPassword, confirmNewPassword = NewPassword,
        };

        for (var i = 0; i < 4; i++)
        {
            await PutAsync(client, Wrong());
        }

        Assert.Equal(HttpStatusCode.OK, (await PutAsync(client, new
        {
            displayName = "p3reset", currentPassword = Password, newPassword = NewPassword, confirmNewPassword = NewPassword,
        })).StatusCode);

        for (var i = 0; i < 4; i++)
        {
            await PutAsync(client, Wrong());
        }

        Assert.Equal(HttpStatusCode.OK, await LoginStatusAsync("p3reset", NewPassword));
    }

    // 情境（SC-003a）：同一組不合規密碼在新增使用者、管理者重設、本人變更三個入口的拒絕訊息一致；
    // 既有密碼不追溯（種子 admin 的 9 碼密碼仍可登入）。以專用角色取得兩個選單權限，不依賴種子執行順序。
    // @SPEC-013 @AC-22
    [Theory(DisplayName = "三個設定密碼入口對不合規密碼的拒絕訊息一致（013，SC-003a）")]
    [InlineData("Abcdefghij1")]
    [InlineData("abcdefghijk1")]
    [InlineData("ABCDEFGHIJK1")]
    [InlineData("Abcdefghijkl")]
    public async Task Password_policy_is_consistent_across_entry_points(string weak)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roleCode = await CreateRoleAsync(factory, $"P3C{suffix}", "accountMaintenance", "personalData");
        var account = $"p3c{suffix}";
        await CreateUserAsync(factory, $"p3t{suffix}");
        var targetId = await UserPublicIdAsync(factory, $"p3t{suffix}");
        await CreateUserAsync(factory, account, roleCode: roleCode);
        var client = await LoginAsync(factory, account);

        var create = await client.PostAsJsonAsync(
            "/users", new { account = $"p3n{suffix}", displayName = "新使用者", password = weak, roleIds = new[] { await RolePublicIdAsync(factory, DefaultRoleCode) } }, Ct);
        var reset = await client.PutAsJsonAsync($"/users/{targetId}/password", new { newPassword = weak }, Ct);
        var profile = await PutAsync(client, new
        {
            displayName = account, currentPassword = Password, newPassword = weak, confirmNewPassword = weak,
        });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, profile.StatusCode);

        var createMessages = (await ErrorsAsync(create))["password"].ToHashSet();
        var resetMessages = (await ErrorsAsync(reset))["newPassword"].ToHashSet();
        var profileMessages = (await ErrorsAsync(profile))["newPassword"].ToHashSet();
        Assert.Equal(createMessages, resetMessages);
        Assert.Equal(createMessages, profileMessages);

        Assert.Equal(HttpStatusCode.OK, await LoginStatusAsync("admin", "Admin123!"));
    }
}
