using System.Net.Http.Json;
using VerifyXunit;

namespace TcmClinic.Api.FunctionalTests.Contracts;

/// <summary>
/// /me/menus 回應契約快照（SPEC-008，Principle XV）。以 admin 登入為最小確定性 fixture：
/// 其可見選單為種子之選單管理（/admin/menus）與業務選單樹（MenuSeedData），形狀固定；鎖定精簡欄位 text/value/subMenu。
/// </summary>
public class MyMenuContractTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境：/me/menus 回應之欄位形狀不得無意間變動——僅 text/value/subMenu，葉節點無 subMenu、無 id/icon/順序/啟用（FR-004，契約快照）。
    // @SPEC-008 @AC-2
    // @SPEC-008 @AC-3
    // @SPEC-008 @AC-4
    // @SPEC-008 @AC-7
    // @SPEC-008 @AC-11
    [Fact(DisplayName = "/me/menus 回應欄位形狀為 text/value/subMenu 且葉節點無 subMenu（FR-004，契約快照）")]
    public async Task My_menu_response_contract()
    {
        var client = factory.CreateClient();
        var login = await AuthTestSupport.PostLoginAsync(client, "admin", "Admin123!", Ct);
        login.EnsureSuccessStatusCode();

        var json = await client.GetStringAsync("/me/menus", Ct);

        await Verifier.VerifyJson(json);
    }
}
