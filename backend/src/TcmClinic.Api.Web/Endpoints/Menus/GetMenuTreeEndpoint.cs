using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Menus;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Menus;

/// <summary>GET /menus — 取得完整選單樹（含停用者，供管理檢視，US1）。</summary>
public sealed class GetMenuTreeEndpoint(IMediator mediator)
    : EndpointWithoutRequest<Ok<IReadOnlyList<MenuTreeDto>>>
{
    public override void Configure()
    {
        Get("/menus");
        Policies("MenuAccess:/admin/menus");
        Summary(s =>
        {
            s.Summary = "取得選單樹";
            s.Description = "回傳完整的後台選單階層（含停用項目），供選單設定頁使用。";
            s.ResponseExamples[200] = new List<MenuTreeDto>
            {
                new(ExampleIds.Menu, "使用者管理", null, "mdi-account-group", 1, true,
                    [new(ExampleIds.Menu2, "使用者清單", "/admin/users", "mdi-account", 1, true, [])]),
            };
            s.Responses[200] = "選單樹";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Menus");
    }

    public override async Task<Ok<IReadOnlyList<MenuTreeDto>>> ExecuteAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMenuTreeQuery(), ct);
        return result.ToOkOnlyResult(items => items);
    }
}
