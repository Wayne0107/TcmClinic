using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.SharedKernel;
using NetArchTest.Rules;

namespace TcmClinic.Api.UnitTests.Architecture;

/// <summary>
/// 以可執行的架構測試強制 Clean Architecture 依賴方向（Principle I / XV）。
/// </summary>
public class ArchitectureTests
{
    private const string Core = "TcmClinic.Api.Core";
    private const string Infrastructure = "TcmClinic.Api.Infrastructure";
    private const string Web = "TcmClinic.Api.Web";

    // 情境：掃描 UseCases 組件（含 Users/Auth 命名空間）的相依 → 不得相依 Infrastructure 或 Web（Principle I）。
    [Fact(DisplayName = "UseCases 不得相依 Infrastructure 或 Web（Principle I）")]
    public void UseCases_should_not_depend_on_Infrastructure_or_Web()
    {
        var result = Types.InAssembly(typeof(TcmClinic.Api.UseCases.Auth.ILoginThrottle).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Web)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    // 情境：掃描 Core 組件的相依 → 不得相依 Infrastructure 或 Web（Principle I 依賴方向）。
    [Fact(DisplayName = "Core 不得相依 Infrastructure 或 Web（Principle I 依賴方向）")]
    public void Core_should_not_depend_on_Infrastructure_or_Web()
    {
        var result = Types.InAssembly(typeof(Role).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Web)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    // 情境：UseCases.Auth 命名空間（005 授權查詢 GetVisibleMenuRoutes）→ 不得相依 Infrastructure 或 Web
    // （Principle I 依賴方向：內層 UseCases 不得反向相依外層；T026）。
    [Fact(DisplayName = "UseCases.Auth 命名空間不得相依 Infrastructure 或 Web（Principle I，T026）")]
    public void UseCasesAuth_should_not_depend_on_Infrastructure_or_Web()
    {
        var result = Types.InAssembly(typeof(TcmClinic.Api.UseCases.Auth.GetVisibleMenuRoutesQuery).Assembly)
            .That()
            .ResideInNamespace("TcmClinic.Api.UseCases.Auth")
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Web)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    // 情境：Web.Authorization 命名空間（MenuAccess 需求/處理器）→ 不得相依 Infrastructure
    // （授權為 Web 橫切面，應經 UseCases/Mediator 抽象而非直接相依基礎設施；Principle I，T026）。
    [Fact(DisplayName = "Web.Authorization 命名空間不得相依 Infrastructure（授權橫切面經抽象，T026）")]
    public void WebAuthorization_should_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(typeof(TcmClinic.Api.Web.Authorization.MenuAccessHandler).Assembly)
            .That()
            .ResideInNamespace("TcmClinic.Api.Web.Authorization")
            .ShouldNot()
            .HaveDependencyOn(Infrastructure)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    // 情境：UseCases 與 Core 全組件 → 不得相依 EF Core（Principle III：資料存取經 Repository／Specification 抽象；
    // 並行衝突等以「儲存失敗後重查」判定，而非捕捉 EF 例外）。
    [Fact(DisplayName = "UseCases 與 Core 不得相依 EF Core（Principle III）")]
    public void UseCases_and_Core_should_not_depend_on_ef_core()
    {
        var useCases = Types.InAssembly(typeof(TcmClinic.Api.UseCases.Auth.ILoginThrottle).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        var core = Types.InAssembly(typeof(Role).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(useCases.IsSuccessful, Format(useCases));
        Assert.True(core.IsSuccessful, Format(core));
    }

    // 情境：掃描 Infrastructure 組件的相依 → 不得相依 Web（Principle I 依賴方向）。
    // Infrastructure 可實作 Core 或 UseCases 定義的介面（如 ILoginThrottle），但不得反向引用最外層 Web。
    [Fact(DisplayName = "Infrastructure 不得相依 Web（Principle I 依賴方向）")]
    public void Infrastructure_should_not_depend_on_Web()
    {
        var result = Types.InAssembly(typeof(TcmClinic.Api.Infrastructure.Data.Security.MemoryLoginThrottle).Assembly)
            .ShouldNot()
            .HaveDependencyOn(Web)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    // 情境：掃描 SharedKernel 組件的相依 → 不得相依任何外層 Core/Infrastructure/Web（Principle I）。
    [Fact(DisplayName = "SharedKernel 不得相依任何外層 Core/Infrastructure/Web（Principle I）")]
    public void SharedKernel_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(typeof(IAggregateRoot).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(Core, Infrastructure, Web)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    private static string Format(NetArchTest.Rules.TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "違規型別：" + string.Join(", ", result.FailingTypeNames ?? []);
}
