using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Reqnroll;

namespace TcmClinic.Api.AcceptanceTests;

/// <summary>
/// 版本號與建置識別.feature 的步驟定義（SPEC-065，docs/TESTING.md §4.6）。
/// 使用真實 Cookie 驗證的 <see cref="AuthWebApplicationFactory"/>，才能驗證「未登入」可呼叫；每個 feature 共用一個工廠。
/// 預期值取自 Web 組件的 <see cref="AssemblyInformationalVersionAttribute"/>（建置時由 SDK 寫入版本號與 commit sha）。
/// </summary>
[Binding]
[Scope(Feature = "版本號與建置識別")]
public sealed partial class VersionSteps(FeatureContext featureContext) : IDisposable
{
    private const string FactoryKey = nameof(VersionSteps) + nameof(AuthWebApplicationFactory);

    private static readonly string AssemblyVersion =
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    private HttpClient? _client;
    private HttpResponseMessage? _response;
    private JsonElement _body;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [BeforeFeature]
    public static async Task StartFactoryAsync(FeatureContext context)
    {
        var factory = new AuthWebApplicationFactory();
        await factory.InitializeAsync();
        context.Set(factory, FactoryKey);
    }

    // 啟動失敗時工廠不在 context 中；不再丟 KeyNotFoundException 蓋掉 BeforeFeature 的真正錯誤。
    [AfterFeature]
    public static async Task StopFactoryAsync(FeatureContext context)
    {
        if (context.TryGetValue(FactoryKey, out AuthWebApplicationFactory factory))
        {
            await factory.DisposeAsync();
        }
    }

    public void Dispose() => _client?.Dispose();

    [Given("我沒有登入")]
    public void GivenAnonymous() =>
        _client = featureContext.Get<AuthWebApplicationFactory>(FactoryKey).CreateClient();

    [When("我查詢建置識別")]
    public async Task WhenGetVersionAsync()
    {
        _response = await _client!.GetAsync("/version", Ct);
        if (_response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(await _response.Content.ReadAsStringAsync(Ct));
            _body = doc.RootElement.Clone();
        }
    }

    [Then("建置識別回應狀態為 {int}")]
    public void ThenStatus(int status) => Assert.Equal((HttpStatusCode)status, _response!.StatusCode);

    [Then("建置識別回應恰好包含欄位 {string}、{string}、{string}")]
    public void ThenExactFields(string a, string b, string c) =>
        Assert.Equal(new[] { a, b, c }.Order(), _body.EnumerateObject().Select(p => p.Name).Order());

    [Then("回應的 version 為 SemVer 格式且等於組件版本")]
    public void ThenVersion()
    {
        var version = _body.GetProperty("version").GetString()!;
        Assert.Matches(SemVer(), version);
        Assert.Equal(AssemblyVersion.Split('+')[0], version);
    }

    [Then("回應的 sha 為 40 碼小寫十六進位且等於組件寫入的 sha")]
    public void ThenSha()
    {
        // 沒有 git 資訊的建置（版本字串沒有 +）無法驗證 sha，先以清楚的斷言失敗。
        Assert.Contains('+', AssemblyVersion);
        var sha = _body.GetProperty("sha").GetString()!;
        Assert.Matches(Sha40(), sha);
        Assert.Equal(AssemblyVersion.Split('+')[1], sha);
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$")]
    private static partial Regex SemVer();

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex Sha40();
}
