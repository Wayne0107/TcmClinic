using System.Net.Http.Json;
using VerifyXunit;

namespace TcmClinic.Api.FunctionalTests.Contracts;

/// <summary>/users 回應契約快照（T081，Principle XV）。新種子 DB 僅含 admin，形狀固定；不含密碼。</summary>
public class UsersContractTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record UserItem(
        Guid Id, string Account, string DisplayName, string[] RoleCodes, string[] RoleNames);

    private sealed record UserList(UserItem[] Items, int Page, int PageSize, int TotalCount);

    // 情境：使用者清單回應之欄位形狀不得無意間變動，且不含任何密碼欄位（FR-009，契約快照）。
    // @SPEC-004 @AC-7
    // @SPEC-004 @AC-9
    [Fact(DisplayName = "使用者清單回應欄位形狀不變且不含密碼欄位（FR-009，契約快照）")]
    public async Task Users_list_response_contract()
    {
        var client = factory.CreateClient();
        var body = await client.GetFromJsonAsync<UserList>("/users?page=1&pageSize=20", Ct);

        await Verifier.Verify(body);
    }
}
