using System.Security.Claims;
using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Navigation;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Navigation;

/// <summary>GET /me/menus — 目前使用者的可見選單樹（US3；SPEC-008 起回應對齊前端 text/value/subMenu）。</summary>
public sealed class GetMyMenuEndpoint(IMediator mediator)
    : EndpointWithoutRequest<Ok<IReadOnlyList<MyMenuNodeDto>>>
{
    public override void Configure()
    {
        Get("/me/menus");
        // 任何已登入使用者皆可（不限超級管理員）。
        Summary(s =>
        {
            s.Summary = "取得我的可見選單";
            s.Description = "依使用者所屬角色回傳可見選單樹（精簡欄位 text/value/subMenu，對齊前端）；" +
                "被授權子項的祖先容器自動顯示，停用者排除；葉節點省略 subMenu。";
            s.ResponseExamples[200] = new List<MyMenuNodeDto>
            {
                new("系統管理作業", "system", [new("帳號維護", "accountMaintenance", null)]),
            };
            s.Responses[200] = "可見選單樹（text/value/subMenu）";
            s.Responses[401] = "未登入";
        });
        Tags("Navigation");
    }

    public override async Task<Ok<IReadOnlyList<MyMenuNodeDto>>> ExecuteAsync(CancellationToken ct)
    {
        var roleCodes = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var result = await mediator.Send(new GetMyMenuQuery(roleCodes), ct);
        return result.ToOkOnlyResult(nodes => nodes);
    }
}
