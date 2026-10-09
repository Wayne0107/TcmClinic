using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Roles;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Roles;

/// <summary>編輯角色請求（角色代碼不可變更）。</summary>
public sealed class UpdateRoleRequest
{
    /// <summary>角色對外識別碼（UUIDv7）（路由參數 {id}）。</summary>
    public Guid Id { get; set; }

    /// <summary>角色名稱，必填，最長 20 字。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>角色說明，選填，最長 200 字；傳 null 會清空原說明。</summary>
    public string? Description { get; set; }
}

/// <summary>PUT /roles/{id} — 編輯角色名稱／說明（US4, FR-014）。</summary>
public sealed class UpdateRoleEndpoint(IMediator mediator)
    : Endpoint<UpdateRoleRequest, Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Put("/roles/{id:guid}");
        // 2026-10-02：舊後台選單 /admin/roles 移除，改以「角色權限維護」選單（rolePermission）授權。
        Policies("MenuAccess:rolePermission");
        Summary(s =>
        {
            s.Summary = "編輯角色";
            s.ExampleRequest = new UpdateRoleRequest { Id = ExampleIds.Role2, Name = "編輯者", Description = "可編輯內容的角色" };
            s.Responses[200] = "更新成功";
            s.Responses[404] = "找不到角色";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Roles");
    }

    public override async Task<Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(UpdateRoleRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateRoleCommand(req.Id, req.Name, req.Description), ct);

        return result.ToUpdateResult();
    }
}
