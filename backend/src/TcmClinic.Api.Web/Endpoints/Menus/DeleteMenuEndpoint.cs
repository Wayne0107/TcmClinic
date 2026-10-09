using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Menus;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Menus;

/// <summary>刪除選單項目請求。</summary>
public sealed class DeleteMenuRequest
{
    /// <summary>要刪除的選單項目對外識別碼（UUIDv7）（路由參數 {id}）；仍有子項時無法刪除。</summary>
    public Guid Id { get; set; }
}

/// <summary>DELETE /menus/{id} — 刪除（軟刪除）選單項目（US1, FR-004）。</summary>
public sealed class DeleteMenuEndpoint(IMediator mediator)
    : Endpoint<DeleteMenuRequest, Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Delete("/menus/{id:guid}");
        Policies("MenuAccess:/admin/menus");
        Summary(s =>
        {
            s.Summary = "刪除選單項目";
            s.Description = "軟刪除；若仍有子項則阻擋（FR-004）。";
            s.Responses[204] = "刪除成功";
            s.Responses[404] = "找不到選單項目";
            s.Responses[400] = "仍有子項，無法刪除";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Menus");
    }

    public override async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(DeleteMenuRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteMenuItemCommand(req.Id), ct);

        return result.ToDeleteResult();
    }
}
