using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 個人資料維護（SPEC-013）功能測試共用工具：以 <see cref="AppDbContext"/> 直接建立指定角色的使用者、
/// 以真實登入取得 Bearer token。/profile 以「登入者本人」為對象，故一律使用資料庫中真實存在的帳號。
/// </summary>
internal static class ProfileTestSupport
{
    /// <summary>符合全系統密碼規則（12 碼以上、含大寫、小寫、數字）的測試密碼。</summary>
    public const string Password = "Passw0rdTest12";

    /// <summary>GET /profile 回應。</summary>
    public sealed record ProfileRole(string Code, string Name);

    /// <summary>GET /profile 回應。</summary>
    public sealed record Profile(
        string Account,
        string DisplayName,
        ProfileRole[] Roles,
        string? Phone,
        string? Email,
        string PasswordChangedAt,
        int PasswordRemainingDays,
        bool PasswordExpired,
        string Status);

    /// <summary>PUT /profile 成功回應。</summary>
    public sealed record UpdateResult(string Message);

    /// <summary>今日（本地）日期字串，格式同契約 yyyy-MM-dd。</summary>
    public static string Today => DateTime.Now.ToString("yyyy-MM-dd");

    /// <summary>依帳號取使用者對外識別碼（SPEC-064；呼叫 /users/{id} 類端點時使用）。</summary>
    public static async Task<Guid> UserPublicIdAsync(WebApplicationFactory<Program> factory, string account) =>
        (await QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .Where(u => u.Account == UserAccount.From(account)).Select(u => u.PublicId).SingleAsync())).Value;

    /// <summary>依代碼取角色對外識別碼（SPEC-064；請求內容的 roleIds 使用）。</summary>
    public static async Task<Guid> RolePublicIdAsync(WebApplicationFactory<Program> factory, string roleCode) =>
        (await QueryDbAsync(factory, ctx => ctx.Roles.AsNoTracking()
            .Where(r => r.Code == RoleCode.From(roleCode)).Select(r => r.PublicId).SingleAsync())).Value;

    /// <summary>預設角色代碼：可見「個人資料維護」的一般使用者角色，首次使用時建立。</summary>
    public const string DefaultRoleCode = "USER";

    /// <summary>預設角色名稱。</summary>
    public const string DefaultRoleName = "一般使用者";

    /// <summary>
    /// 建立一名指派指定角色的使用者，回傳其內部 Id。未指定角色時使用 <see cref="DefaultRoleCode"/>
    /// （不存在則建立並授予 personalData 可見性）；指定其他角色時，該角色須已存在。
    /// </summary>
    public static async Task<int> CreateUserAsync(
        WebApplicationFactory<Program> factory,
        string account,
        string roleCode = DefaultRoleCode,
        string? displayName = null,
        string password = Password)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var role = await ctx.Roles.SingleOrDefaultAsync(r => r.Code == RoleCode.From(roleCode))
            ?? (roleCode == DefaultRoleCode
                ? await CreateDefaultRoleAsync(ctx)
                : throw new InvalidOperationException($"角色 {roleCode} 不存在。"));
        var (hash, salt) = hasher.Hash(password);
        var user = new User(UserAccount.From(account), UserName.From(displayName ?? account))
            .SetPassword(hash, salt, DateTime.Now)
            .ReplaceRoles([role.Id]);
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id.Value;
    }

    /// <summary>建立預設角色並授予「個人資料維護」可見性。</summary>
    private static async Task<Role> CreateDefaultRoleAsync(AppDbContext ctx)
    {
        var role = new Role(RoleCode.From(DefaultRoleCode), RoleName.From(DefaultRoleName));
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync();

        var menu = await ctx.MenuItems.SingleAsync(m => m.Route == "personalData");
        ctx.RoleMenuVisibilities.Add(new RoleMenuVisibility(role.Id, menu.Id));
        await ctx.SaveChangesAsync();
        return role;
    }

    /// <summary>建立一個不具任何選單可見性的自訂角色，回傳其代碼。</summary>
    public static async Task<string> CreateRoleAsync(
        WebApplicationFactory<Program> factory, string code, params string[] visibleRoutes)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var role = new Role(RoleCode.From(code), RoleName.From(code));
        ctx.Roles.Add(role);
        await ctx.SaveChangesAsync();

        foreach (var route in visibleRoutes)
        {
            var menu = await ctx.MenuItems.SingleAsync(m => m.Route == route);
            ctx.RoleMenuVisibilities.Add(new RoleMenuVisibility(role.Id, menu.Id));
        }

        await ctx.SaveChangesAsync();
        return code;
    }

    /// <summary>以帳號密碼登入，回傳帶 Bearer access token（與 refresh Cookie）的用戶端。</summary>
    public static async Task<HttpClient> LoginAsync(
        WebApplicationFactory<Program> factory, string account, string password = Password)
    {
        var client = factory.CreateClient();
        // 2026-10-08 起登入回 JWT（ADR-011）：輔助方法替用戶端帶上 Bearer。
        var login = await AuthTestSupport.PostLoginAsync(client, account, password);
        login.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>在獨立 scope 中讀寫資料庫。</summary>
    public static async Task WithDbAsync(WebApplicationFactory<Program> factory, Func<AppDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(ctx);
    }

    /// <summary>在獨立 scope 中查詢資料庫。</summary>
    public static async Task<T> QueryDbAsync<T>(WebApplicationFactory<Program> factory, Func<AppDbContext, Task<T>> query)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(ctx);
    }
}
