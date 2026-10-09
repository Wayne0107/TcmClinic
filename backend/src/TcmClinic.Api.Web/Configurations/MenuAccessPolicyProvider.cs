using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TcmClinic.Api.Web.Authorization;

namespace TcmClinic.Api.Web.Configurations;

/// <summary>
/// 動態授權原則供應器：將端點宣告的 <c>"MenuAccess:{routes}"</c> 原則名解析為
/// <see cref="MenuAccessRequirement"/>。<c>routes</c> 以 <c>|</c> 分隔多值表 OR
/// （如 <c>MenuAccess:accountMaintenance|rolePermission</c>）。其餘原則名回退至預設供應器。
/// </summary>
public sealed class MenuAccessPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "MenuAccess:";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var routes = policyName[Prefix.Length..]
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new MenuAccessRequirement(routes))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}
