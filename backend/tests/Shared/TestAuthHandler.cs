using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Web.Authorization;

namespace TcmClinic.Api.Tests.Shared;

/// <summary>
/// 測試用驗證處理器（005）：以測試身分登入，並注入「可見選單 route」claims 供 MenuAccess 授權。
/// 預設授予全部管理頁（帳號維護 accountMaintenance、角色權限維護 rolePermission、選單管理 /admin/menus），使既有 CRUD 測試維持全權；
/// 授權矩陣測試可用請求標頭 <see cref="MenusHeader"/>（逗號分隔 route；空字串＝無任何可見）覆寫。
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    /// <summary>覆寫可見選單 route 的請求標頭（逗號分隔；存在但空值＝無任何可見）。</summary>
    public const string MenusHeader = "X-Test-Menus";

    private static readonly string[] AllAdminRoutes =
        ["accountMaintenance", "rolePermission", "/admin/menus"]; // 使用者／角色端點的選單授權值

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var routes = Request.Headers.TryGetValue(MenusHeader, out var header)
            ? header.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : AllAdminRoutes;

        // Role claim 供 /me/menus（GetMyMenu 依角色代碼聚合）；menu claims 供 MenuAccess 授權。
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test-admin"),
            new(ClaimTypes.Role, Role.SuperAdminCode),
        };
        foreach (var route in routes)
        {
            claims.Add(new Claim(MenuAccessHandler.MenuClaimType, route));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
