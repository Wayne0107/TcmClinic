using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace TcmClinic.Api.Web.Endpoints.Auth;

/// <summary>目前登入者身分。</summary>
/// <param name="Account">登入帳號（小寫）。</param>
/// <param name="DisplayName">顯示名稱；每次請求依最新資料更新，改名後立即反映。</param>
/// <param name="Roles">目前所屬角色代碼清單（例如 SUPERADMIN）；每次請求依最新角色更新。</param>
public sealed record MeResponse(string Account, string DisplayName, IReadOnlyList<string> Roles);

/// <summary>GET /auth/me — 目前登入者身分與現時角色（US1）。角色經 R8 每請求刷新。</summary>
public sealed class MeEndpoint : EndpointWithoutRequest<Ok<MeResponse>>
{
    public override void Configure()
    {
        Get("/auth/me");
        Summary(s =>
        {
            s.Summary = "目前登入者";
            s.ResponseExamples[200] = new MeResponse("superadmin", "系統管理員", ["SUPERADMIN"]);
            s.Responses[200] = "目前身分與角色";
            s.Responses[401] = "未登入";
        });
        Tags("Auth");
    }

    public override Task<Ok<MeResponse>> ExecuteAsync(CancellationToken ct)
    {
        var account = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var displayName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Task.FromResult<Ok<MeResponse>>(
            TypedResults.Ok(new MeResponse(account, displayName, roles)));
    }
}
