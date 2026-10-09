using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.UserAggregate;
using static TcmClinic.Api.FunctionalTests.ProfileTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 回歸測試（SPEC-013，設計決策 R14）：Web 的 <c>ICurrentUser</c> 必須是 HttpContext 版本。
/// 曾因 Program.cs 註冊順序錯誤而被 Infrastructure 預設的 SystemCurrentUser 覆蓋，使稽核欄位與異動紀錄恆記為 "system"。
/// 使用真實 Cookie 登入（AuthWebApplicationFactory）。需 Docker Desktop。
/// </summary>
public class CurrentUserResolutionTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境（設計決策 R14）：登入者修改個人資料 → SysUser.ModifyUser 與該筆 SysAuditLog.CreateUser 皆為登入帳號，
    // 而非 system／anonymous（若 ICurrentUser 註冊順序迴歸，本測試轉紅）。
    // @SPEC-006 @AC-11
    // @SPEC-013 @AC-32
    [Fact(DisplayName = "已登入請求的稽核欄位與異動紀錄記錄真實登入帳號（013，設計決策 R14）")]
    public async Task Authenticated_request_records_real_account()
    {
        var id = await CreateUserAsync(factory, "r14user");
        var client = await LoginAsync(factory, "r14user");

        var res = await client.PutAsJsonAsync("/profile", new { displayName = "稽核帳號驗證" }, Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var modifyUser = await QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .Where(u => u.Account == UserAccount.From("r14user")).Select(u => u.ModifyUser).SingleAsync(Ct));
        Assert.Equal("r14user", modifyUser);

        var pk = id.ToString();
        var operatedBy = await QueryDbAsync(factory, ctx => ctx.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == "User" && a.PrimaryKey == pk && a.Operation == "Update")
            .Select(a => a.CreateUser).ToListAsync(Ct));
        Assert.Contains("r14user", operatedBy);
        Assert.DoesNotContain("system", operatedBy);
        Assert.DoesNotContain("anonymous", operatedBy);
    }

    // 情境（設計決策 R8 2026-09-30 修訂）：SysUserLog 的操作者改由攔截器自獨立 scope 的 ICurrentUser 填入 CreateUser／ModifyUser；
    // 該 scope 仍在同一請求的非同步流程中，必須解析為登入帳號，而非 system／anonymous（若獨立 scope 取不到 HttpContext，本測試轉紅）。
    // @SPEC-013 @AC-32
    [Fact(DisplayName = "獨立 scope 寫入的操作紀錄以登入帳號為建立者（013，設計決策 R8）")]
    public async Task User_log_written_in_separate_scope_records_real_account()
    {
        await CreateUserAsync(factory, "r8user");
        var client = await LoginAsync(factory, "r8user");

        var res = await client.PutAsJsonAsync("/profile", new { displayName = "操作紀錄建立者驗證" }, Ct);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var log = await QueryDbAsync(factory, ctx => ctx.UserLogs.AsNoTracking()
            .Where(l => l.FunctionId == "PersonalData" && l.Content.Contains("(r8user)"))
            .SingleAsync(Ct));
        Assert.Equal("r8user", log.CreateUser);
        Assert.Equal("r8user", log.ModifyUser);
    }

    // 情境（設計決策 R14）：啟動種子無 HttpContext → 種子列的建立者仍標示為 system（與修正前一致）。
    // @SPEC-013 @AC-32
    [Fact(DisplayName = "啟動種子建立的資料列建立者仍為 system（013，設計決策 R14）")]
    public async Task Seeded_rows_are_created_by_system()
    {
        var createUser = await QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .Where(u => u.Account == UserAccount.From("admin")).Select(u => u.CreateUser).SingleAsync(Ct));

        Assert.Equal("system", createUser);
    }
}
