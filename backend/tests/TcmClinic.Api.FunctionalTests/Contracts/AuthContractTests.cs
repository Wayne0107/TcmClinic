using System.Net.Http.Json;
using VerifyXunit;

namespace TcmClinic.Api.FunctionalTests.Contracts;

/// <summary>/auth 回應契約快照（T028，Principle XV）。鎖定 LoginResponse 欄位形狀。</summary>
public class AuthContractTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record LoginResp(
        string AccessToken, string TokenType, int ExpiresIn, string Account, string DisplayName, string[] Roles);

    // 情境：登入回應之欄位形狀（accessToken/tokenType/expiresIn/account/displayName/roles）不得無意間變動（契約快照）；
    // access token 每次不同，快照中以佔位字代替（2026-10-08 起為 JWT，ADR-011）。
    // @SPEC-004 @AC-2
    [Fact(DisplayName = "登入回應欄位形狀含 access token 與身分摘要不變（契約快照）")]
    public async Task Login_response_contract()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync(
            "/auth/login", new { account = "admin", password = "Admin123!" }, Ct);
        var body = await res.Content.ReadFromJsonAsync<LoginResp>(Ct);

        await Verifier.Verify(body! with { AccessToken = body.AccessToken.Length > 0 ? "{jwt}" : string.Empty });
    }
}
