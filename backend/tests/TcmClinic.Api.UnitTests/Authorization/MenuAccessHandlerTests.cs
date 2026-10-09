using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TcmClinic.Api.Web.Authorization;

namespace TcmClinic.Api.UnitTests.Authorization;

/// <summary>
/// MenuAccessHandler（005 對稱模型，T003）：比對使用者 <c>menu</c> claims 與需求 route，
/// 任一命中即通過（OR 語意，供 B 方案）；比對大小寫不敏感。
/// </summary>
public class MenuAccessHandlerTests
{
    // 以指定的可見 route 建立帶 menu claims 的使用者主體。
    private static ClaimsPrincipal UserWithMenus(params string[] routes)
    {
        var identity = new ClaimsIdentity(
            routes.Select(r => new Claim(MenuAccessHandler.MenuClaimType, r)), "test");
        return new ClaimsPrincipal(identity);
    }

    // 對指定使用者與需求 route 執行授權，回傳是否通過。
    private static async Task<bool> EvaluateAsync(ClaimsPrincipal user, params string[] required)
    {
        var requirement = new MenuAccessRequirement(required);
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);
        await new MenuAccessHandler().HandleAsync(context);
        return context.HasSucceeded;
    }

    // 情境：menu claims 含所需 route → 授權通過。
    [Fact(DisplayName = "menu claims 含所需 route 時授權通過")]
    public async Task Succeeds_when_menu_claim_matches()
        => Assert.True(await EvaluateAsync(UserWithMenus("/admin/users"), "/admin/users"));

    // 情境：menu claims 不含所需 route → 不通過（SC-002 無可見性即拒絕）。
    // @SPEC-005 @AC-2
    [Fact(DisplayName = "menu claims 不含所需 route 時不通過（SC-002）")]
    public async Task Fails_when_menu_claim_absent()
        => Assert.False(await EvaluateAsync(UserWithMenus("/admin/roles"), "/admin/users"));

    // 情境：需求多 route（B 方案 OR）——使用者具其一即通過。
    // @SPEC-005 @AC-2
    [Fact(DisplayName = "需求多 route 時具其一即通過（B 方案 OR 語意）")]
    public async Task Succeeds_when_any_of_required_routes_matches()
        => Assert.True(await EvaluateAsync(UserWithMenus("/admin/users"), "/admin/users", "/admin/roles"));

    // 情境：完全無 menu claims → 不通過。
    [Fact(DisplayName = "完全無 menu claims 時不通過")]
    public async Task Fails_when_no_menu_claims()
        => Assert.False(await EvaluateAsync(UserWithMenus(), "/admin/users"));

    // 情境：route 比對大小寫不敏感（OrdinalIgnoreCase）→ 通過。
    [Fact(DisplayName = "route 比對大小寫不敏感時通過")]
    public async Task Succeeds_case_insensitively()
        => Assert.True(await EvaluateAsync(UserWithMenus("/Admin/Users"), "/admin/users"));
}
