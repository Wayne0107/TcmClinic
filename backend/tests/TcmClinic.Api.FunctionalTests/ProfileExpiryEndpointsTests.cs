using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using static TcmClinic.Api.FunctionalTests.ProfileTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>效期天數設為 60 的測試工廠（驗證設定來源可調整，FR-011）。</summary>
public class PasswordValidDays60Factory : AuthWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("PasswordPolicy:ValidDays", "60");
    }
}

/// <summary>效期天數設為 0（不合法）的測試工廠：應用程式啟動時應被 Options 驗證擋下。</summary>
public class PasswordValidDaysZeroFactory : AuthWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("PasswordPolicy:ValidDays", "0");
    }
}

/// <summary>
/// 個人資料維護（SPEC-013）密碼有效期限的功能測試（US4，FR-011、FR-012、SC-005）。需 Docker Desktop。
/// 日期以應用程式本地時間計算後以參數寫入（容器時鐘為 UTC，不可用 SQL 的 GETDATE）。
/// </summary>
public class ProfileExpiryEndpointsTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task SetPasswordChangedAtAsync(string account, DateTime? changedAt, DateTime? createDate = null) =>
        WithDbAsync(factory, async ctx =>
        {
            if (changedAt is null)
            {
                await ctx.Database.ExecuteSqlRawAsync(
                    "UPDATE SysUser SET PasswordChangedAt = NULL WHERE Account = {0}", new object[] { account }, Ct);
            }
            else
            {
                await ctx.Database.ExecuteSqlRawAsync(
                    "UPDATE SysUser SET PasswordChangedAt = {0} WHERE Account = {1}",
                    new object[] { changedAt.Value, account }, Ct);
            }
            if (createDate is not null)
            {
                await ctx.Database.ExecuteSqlRawAsync(
                    "UPDATE SysUser SET CreateDate = {0} WHERE Account = {1}", new object[] { createDate.Value, account }, Ct);
            }
        });

    // 情境（US4/AC2、SC-005）：效期 90 天、31 天前修改密碼 → 剩 59 天、未逾期（對齊舊系統畫面）。
    // @SPEC-013 @AC-18
    [Fact(DisplayName = "31 天前修改密碼時剩餘 59 天（013，US4/AC2、SC-005）")]
    public async Task Changed_31_days_ago_leaves_59_days()
    {
        await CreateUserAsync(factory, "p4d31");
        await SetPasswordChangedAtAsync("p4d31", DateTime.Now.AddDays(-31));
        var client = await LoginAsync(factory, "p4d31");

        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);

        Assert.Equal(DateTime.Now.AddDays(-31).ToString("yyyy-MM-dd"), profile!.PasswordChangedAt);
        Assert.Equal(59, profile.PasswordRemainingDays);
        Assert.False(profile.PasswordExpired);
    }

    // 情境（US4/AC3、FR-012）：90 天前修改 → 剩 0 天且標示逾期；逾期者仍可登入並修改個人資料（僅顯示、不限制）。
    // @SPEC-013 @AC-19
    [Fact(DisplayName = "逾期帳號剩餘 0 天並標示逾期，但仍可登入與修改（013，US4/AC3、FR-012）")]
    public async Task Expired_password_is_display_only()
    {
        await CreateUserAsync(factory, "p4expired");
        await SetPasswordChangedAtAsync("p4expired", DateTime.Now.AddDays(-90));
        var client = await LoginAsync(factory, "p4expired");

        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);
        Assert.Equal(0, profile!.PasswordRemainingDays);
        Assert.True(profile.PasswordExpired);

        var res = await client.PutAsJsonAsync("/profile", new { displayName = "逾期仍可修改" }, Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // 情境（Edge Case、設計決策 R5）：上線前建立的帳號沒有修改紀錄 → 以建立日計算（10 天前建立 → 剩 80 天）。
    // @SPEC-013 @AC-26
    [Fact(DisplayName = "無密碼修改紀錄的既有帳號以建立日計算（013，Edge Case）")]
    public async Task Missing_changed_at_falls_back_to_create_date()
    {
        await CreateUserAsync(factory, "p4legacy");
        var created = DateTime.Now.AddDays(-10);
        await SetPasswordChangedAtAsync("p4legacy", null, created);
        var client = await LoginAsync(factory, "p4legacy");

        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);

        Assert.Equal(created.ToString("yyyy-MM-dd"), profile!.PasswordChangedAt);
        Assert.Equal(80, profile.PasswordRemainingDays);
    }
}

/// <summary>效期天數可由設定調整（SPEC-013，FR-011）。</summary>
public class ProfileExpiryConfigurationTests(PasswordValidDays60Factory factory)
    : IClassFixture<PasswordValidDays60Factory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境（FR-011）：設定 PasswordPolicy:ValidDays=60 → 當日修改密碼者剩 60 天。
    // @SPEC-013 @AC-26
    [Fact(DisplayName = "效期設定為 60 天時剩餘天數依 60 天計算（013，FR-011）")]
    public async Task Valid_days_setting_is_applied()
    {
        await CreateUserAsync(factory, "p4sixty");
        var client = await LoginAsync(factory, "p4sixty");

        var profile = await client.GetFromJsonAsync<Profile>("/profile", Ct);

        Assert.Equal(60, profile!.PasswordRemainingDays);
    }
}

/// <summary>不合法的效期設定於啟動時被擋下（SPEC-013，FR-011）。</summary>
public class ProfileExpiryInvalidConfigurationTests(PasswordValidDaysZeroFactory factory)
    : IClassFixture<PasswordValidDaysZeroFactory>
{
    // 情境：PasswordPolicy:ValidDays=0 → 應用程式啟動失敗，錯誤指出 ValidDays（Options 驗證）。
    // @SPEC-013 @AC-26
    [Fact(DisplayName = "效期設定為 0 時應用程式啟動失敗（013，FR-011）")]
    public void Zero_valid_days_fails_on_start()
    {
        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("ValidDays", ex.ToString());
    }
}
