using Microsoft.AspNetCore.Authorization;

namespace TcmClinic.Api.Web.Authorization;

/// <summary>
/// 授權需求：使用者須對「所列 route 之任一」具選單可見性（005 對稱模型）。
/// 多 route 表 OR（供 B 方案：如 GET /roles 允許 accountMaintenance 或 rolePermission）。
/// </summary>
public sealed class MenuAccessRequirement(IReadOnlyList<string> anyOfRoutes) : IAuthorizationRequirement
{
    public IReadOnlyList<string> AnyOfRoutes { get; } = anyOfRoutes;
}

/// <summary>
/// 比對使用者之 <c>menu</c> claims（由 Cookie 每請求重驗時寫入，見 AuthConfigs）與需求 route；
/// 任一命中即通過。
/// </summary>
public sealed class MenuAccessHandler : AuthorizationHandler<MenuAccessRequirement>
{
    /// <summary>承載「使用者可見選單 route」的 claim 型別。</summary>
    public const string MenuClaimType = "menu";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MenuAccessRequirement requirement)
    {
        var visible = context.User
            .FindAll(MenuClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requirement.AnyOfRoutes.Any(visible.Contains))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
