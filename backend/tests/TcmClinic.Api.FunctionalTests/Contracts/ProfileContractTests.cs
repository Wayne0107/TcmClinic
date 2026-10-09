using System.Text.Json;
using VerifyXunit;
using static TcmClinic.Api.FunctionalTests.ProfileTestSupport;

namespace TcmClinic.Api.FunctionalTests.Contracts;

/// <summary>
/// GET /profile 回應契約快照（SPEC-013，API 契約 §1）。只快照「欄位名稱與 JSON 值型別」，
/// 不含日期等會變動的值，使快照穩定且能攔下欄位新增、改名或型別改變。
/// </summary>
public class ProfileContractTests(AuthWebApplicationFactory factory)
    : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>將 JSON 轉為「欄位 → 值型別」的形狀描述（陣列以首個元素代表）。</summary>
    private static object Shape(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(p => p.Name, p => Shape(p.Value)),
        JsonValueKind.Array => element.GetArrayLength() == 0
            ? new List<object>()
            : new List<object> { Shape(element[0]) },
        JsonValueKind.True or JsonValueKind.False => "Boolean",
        _ => element.ValueKind.ToString(),
    };

    // 情境：個人資料回應之欄位名稱與型別不得無意間變動（FR-017，契約快照）。
    // @SPEC-013 @AC-30
    [Fact(DisplayName = "個人資料回應欄位名稱與型別不變（013，FR-017 契約快照）")]
    public async Task Profile_response_contract()
    {
        await CreateUserAsync(factory, "p1contract");
        var client = await LoginAsync(factory, "p1contract");

        var raw = await client.GetStringAsync("/profile", Ct);
        using var doc = JsonDocument.Parse(raw);

        await Verifier.Verify(Shape(doc.RootElement));
    }
}
