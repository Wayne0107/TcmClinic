using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Menus;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Menus;

/// <summary>啟用／停用選單項目請求。</summary>
public sealed class SetMenuEnabledRequest
{
    /// <summary>選單項目對外識別碼（UUIDv7）（路由參數 {id}）。</summary>
    public Guid Id { get; set; }

    /// <summary>true 為啟用、false 為停用；停用的選單不會出現在任何使用者的側邊選單。</summary>
    public bool IsEnabled { get; set; }
}

/// <summary>PATCH /menus/{id}/enabled — 啟用/停用選單項目（US1, FR-006）。</summary>
public sealed class SetMenuEnabledEndpoint(IMediator mediator)
    : Endpoint<SetMenuEnabledRequest, Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Patch("/menus/{id:guid}/enabled");
        Policies("MenuAccess:/admin/menus");
        Summary(s =>
        {
            s.Summary = "啟用／停用選單項目";
            s.ExampleRequest = new SetMenuEnabledRequest { Id = ExampleIds.Menu, IsEnabled = false };
            s.Responses[200] = "更新成功";
            s.Responses[404] = "找不到選單項目";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Menus");
    }

    public override async Task<Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(SetMenuEnabledRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new SetMenuItemEnabledCommand(req.Id, req.IsEnabled), ct);

        return result.ToUpdateResult();
    }
}
