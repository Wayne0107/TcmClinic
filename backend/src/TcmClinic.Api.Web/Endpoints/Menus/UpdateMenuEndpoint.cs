using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Menus;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Menus;

/// <summary>編輯選單項目請求（不變更父層與順序）。</summary>
public sealed class UpdateMenuRequest
{
    /// <summary>選單項目對外識別碼（UUIDv7）（路由參數 {id}）。</summary>
    public Guid Id { get; set; }

    /// <summary>選單名稱，必填，最長 30 字；同一父層下不可重複。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>前端選單識別鍵（路由／value），選填，最長 200 字；傳 null 或空白會清空。</summary>
    public string? Route { get; set; }

    /// <summary>選單圖示識別字，選填，最長 50 字；傳 null 會清空。</summary>
    public string? Icon { get; set; }
}

/// <summary>PUT /menus/{id} — 編輯選單項目（US1, FR-003）。</summary>
public sealed class UpdateMenuEndpoint(IMediator mediator)
    : Endpoint<UpdateMenuRequest, Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Put("/menus/{id:guid}");
        Policies("MenuAccess:/admin/menus");
        Summary(s =>
        {
            s.Summary = "編輯選單項目";
            s.ExampleRequest = new UpdateMenuRequest
            {
                Id = ExampleIds.Menu,
                Name = "使用者管理",
                Route = "/admin/users",
                Icon = "mdi-account",
            };
            s.Responses[200] = "更新成功";
            s.Responses[404] = "找不到選單項目";
            s.Responses[400] = "驗證失敗";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Menus");
    }

    public override async Task<Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(UpdateMenuRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateMenuItemCommand(req.Id, req.Name, req.Route, req.Icon), ct);

        return result.ToUpdateResult();
    }
}
