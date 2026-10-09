using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Menus;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Menus;

/// <summary>新增選單項目請求。</summary>
/// <param name="Name">選單名稱，必填，最長 30 字；同一父層下不可重複。</param>
/// <param name="ParentId">父選單對外識別碼（UUIDv7）；null 表示頂層；有值時父選單必須存在。</param>
/// <param name="Route">前端選單識別鍵（路由／value），選填，最長 200 字；純分組容器可省略，空白視為未填。</param>
/// <param name="Icon">選單圖示識別字，選填，最長 50 字。</param>
/// <param name="DisplayOrder">同層顯示順序，數字小者排前。</param>
public sealed record CreateMenuRequest(
    string Name, Guid? ParentId, string? Route, string? Icon, int DisplayOrder);

/// <summary>新增選單項目回應。</summary>
/// <param name="Id">新建選單項目的對外識別碼（UUIDv7）。</param>
public sealed record CreateMenuResponse(Guid Id);

/// <summary>POST /menus — 新增選單項目（US1, FR-001/002/007/019）。</summary>
public sealed class CreateMenuEndpoint(IMediator mediator)
    : Endpoint<CreateMenuRequest, Results<Created<CreateMenuResponse>, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Post("/menus");
        Policies("MenuAccess:/admin/menus");
        Summary(s =>
        {
            s.Summary = "新增選單項目";
            s.Description = "在指定父層下新增選單；同層名稱不可重複。父容器可不填路由。";
            s.ExampleRequest = new CreateMenuRequest("使用者管理", null, "/admin/users", "mdi-account", 1);
            s.ResponseExamples[201] = new CreateMenuResponse(ExampleIds.Menu);
            s.Responses[201] = "建立成功";
            s.Responses[400] = "驗證失敗（同層名稱重複等）";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Menus");
    }

    public override async Task<Results<Created<CreateMenuResponse>, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(CreateMenuRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateMenuItemCommand(req.Name, req.ParentId, req.Route, req.Icon, req.DisplayOrder), ct);

        return result.ToCreatedResult(id => $"/menus/{id}", id => new CreateMenuResponse(id));
    }
}
