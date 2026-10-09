using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Infrastructure.Data;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 角色相關功能測試的共用資料：開發種子只有 SUPERADMIN，測試另以資料庫直接建立範例角色——
/// 一個 System 類角色（API 只能新增 Custom，SPEC-012）與兩個 Custom 角色。冪等，可在每個測試類別初始化時呼叫。
/// </summary>
internal static class RoleTestSupport
{
    /// <summary>System 類範例角色代碼（不可刪除，SPEC-012 FR-013）。</summary>
    public const string SystemRoleCode = "SYSADMIN";

    private static readonly (string Code, string Name, RoleType Type)[] SampleRoles =
    [
        (SystemRoleCode, "系統管理員", RoleType.System),
        ("CLERK", "經辦", RoleType.Custom),
        ("MANAGER", "主管", RoleType.Custom),
    ];

    /// <summary>範例角色數（不含 SUPERADMIN）。</summary>
    public static int SampleRoleCount => SampleRoles.Length;

    public static async Task EnsureSampleRolesAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = (await ctx.Roles.IgnoreQueryFilters().Select(r => r.Code).ToListAsync())
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var (code, name, type) in SampleRoles.Where(r => !existing.Contains(r.Code)))
        {
            var role = new Role(RoleCode.From(code), RoleName.From(name));
            if (type != RoleType.Custom)
            {
                role.UpdateRoleType(type);
            }

            ctx.Roles.Add(role);
        }

        await ctx.SaveChangesAsync();
    }
}
