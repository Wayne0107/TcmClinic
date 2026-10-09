using System.Net;
using System.Text.Json;
using Ardalis.Specification;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.SharedKernel;
using static TcmClinic.Api.Tests.Shared.MenuRequestTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 所有錯誤回應一律為 RFC 9457 ProblemDetails（docs/API-CONVENTIONS.md §3、DEF-011）：
/// 找不到資源、驗證器擋下、未登入、無權限、未被攔截的例外都不得回空 body 或其他形狀。
/// </summary>
public class ProblemDetailsResponseTests(CustomWebApplicationFactory factory, AuthWebApplicationFactory authFactory)
    : IClassFixture<CustomWebApplicationFactory>, IClassFixture<AuthWebApplicationFactory>
{
    private const string RoleMenu = "rolePermission";
    private const string UserMenu = "accountMaintenance";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string MissingRoleVisibilityUrl => $"/roles/{Guid.CreateVersion7()}/menu-visibility";

    /// <summary>斷言回應為 ProblemDetails：內容類型 problem+json，且 status 與實際狀態碼一致、title 非空。</summary>
    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage res, HttpStatusCode expected)
    {
        Assert.Equal(expected, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(Ct));
        var root = doc.RootElement.Clone();
        Assert.Equal((int)expected, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        return root;
    }

    // 情境：查無資源（用例回 NotFound）時回 404 ProblemDetails，前端可統一讀 title 顯示，而非空 body。
    [Fact(DisplayName = "查無資源回 404 ProblemDetails")]
    public async Task Not_found_resource_returns_problem_details()
    {
        var res = await factory.CreateClient().SendAsync(Request(HttpMethod.Get, MissingRoleVisibilityUrl, RoleMenu), Ct);

        await AssertProblemAsync(res, HttpStatusCode.NotFound);
    }

    // 情境：已登入者呼叫不存在的路由，回 404 ProblemDetails（框架層的 404 同樣不得是空 body）。
    [Fact(DisplayName = "不存在的路由回 404 ProblemDetails")]
    public async Task Unknown_route_returns_problem_details()
    {
        var res = await factory.CreateClient().SendAsync(Request(HttpMethod.Get, "/no-such-resource", RoleMenu), Ct);

        await AssertProblemAsync(res, HttpStatusCode.NotFound);
    }

    // 情境：無該選單權限者被授權擋下，回 403 ProblemDetails。
    [Fact(DisplayName = "無權限回 403 ProblemDetails")]
    public async Task Forbidden_returns_problem_details()
    {
        var res = await factory.CreateClient().SendAsync(Request(HttpMethod.Get, "/users", RoleMenu), Ct);

        await AssertProblemAsync(res, HttpStatusCode.Forbidden);
    }

    // 情境：未登入（真實 JWT Bearer scheme）呼叫受保護端點，回 401 ProblemDetails。
    [Fact(DisplayName = "未登入回 401 ProblemDetails")]
    public async Task Unauthorized_returns_problem_details()
    {
        var client = authFactory.CreateClient(new() { AllowAutoRedirect = false });

        var res = await client.GetAsync("/users", Ct);

        await AssertProblemAsync(res, HttpStatusCode.Unauthorized);
    }

    // 情境：FastEndpoints 驗證器擋下的 400，與用例 Result.Invalid 同為 ValidationProblem：
    // errors 為「欄位 → 訊息陣列」，欄位名稱 camelCase。
    [Fact(DisplayName = "驗證器擋下回 400 ValidationProblem 且 errors 為欄位對訊息陣列")]
    public async Task Validator_failure_returns_validation_problem()
    {
        var body = new Dictionary<string, object?>
        {
            ["account"] = "",
            ["displayName"] = "缺帳號的使用者",
            ["password"] = "Passw0rd!Passw0rd",
            ["roleIds"] = new[] { Guid.CreateVersion7() },
        };

        var res = await factory.CreateClient().SendAsync(Request(HttpMethod.Post, "/users", UserMenu, body), Ct);

        var problem = await AssertProblemAsync(res, HttpStatusCode.BadRequest);
        var messages = problem.GetProperty("errors").GetProperty("account");
        Assert.Equal(JsonValueKind.Array, messages.ValueKind);
        Assert.NotEmpty(messages.EnumerateArray());
    }

    // 情境：用例未攔截的例外由全域例外處理轉成 500 ProblemDetails，且不外洩例外訊息。
    [Fact(DisplayName = "未攔截的例外回 500 ProblemDetails 且不外洩例外訊息")]
    public async Task Unhandled_exception_returns_problem_details()
    {
        using var failing = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<IReadRepository<Role>, ThrowingRoleRepository>()));

        var res = await failing.CreateClient().SendAsync(Request(HttpMethod.Get, MissingRoleVisibilityUrl, RoleMenu), Ct);

        await AssertProblemAsync(res, HttpStatusCode.InternalServerError);
        Assert.DoesNotContain(ThrowingRoleRepository.Message, await res.Content.ReadAsStringAsync(Ct));
    }

    private sealed class ThrowingRoleRepository(AppDbContext dbContext) : EfRepository<Role>(dbContext)
    {
        public const string Message = "模擬資料庫連線中斷";

        public override Task<Role?> FirstOrDefaultAsync(ISpecification<Role> specification, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(Message);
    }
}
