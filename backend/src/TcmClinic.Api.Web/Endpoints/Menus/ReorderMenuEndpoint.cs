using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Menus;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Menus;

/// <summary>調整同層選單順序請求。</summary>
/// <param name="ParentId">要調整的父層選單對外識別碼（UUIDv7）；null 表示頂層。</param>
/// <param name="OrderedIds">依新順序排列的選單項目對外識別碼（UUIDv7）（由前到後），每個識別碼 都必須屬於此父層；未列出的項目維持原順序值。</param>
public sealed record ReorderMenuRequest(Guid? ParentId, List<Guid> OrderedIds);

/// <summary>PUT /menus/reorder — 調整同層顯示順序（US1, FR-005）。</summary>
public sealed class ReorderMenuEndpoint(IMediator mediator)
    : Endpoint<ReorderMenuRequest, Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Put("/menus/reorder");
        Policies("MenuAccess:/admin/menus");
        Summary(s =>
        {
            s.Summary = "調整選單順序";
            s.ExampleRequest = new ReorderMenuRequest(null, [ExampleIds.Menu3, ExampleIds.Menu, ExampleIds.Menu2]);
            s.Responses[200] = "更新成功";
            s.Responses[400] = "id 不屬於此父層";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Menus");
    }

    public override async Task<Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(ReorderMenuRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ReorderMenuItemsCommand(req.ParentId, req.OrderedIds), ct);

        return result.ToUpdateResult();
    }
}
