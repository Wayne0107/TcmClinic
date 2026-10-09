using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Roles;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Roles;

/// <summary>刪除角色請求。</summary>
public sealed class DeleteRoleRequest
{
    /// <summary>要刪除的角色對外識別碼（UUIDv7）（路由參數 {id}）。</summary>
    public Guid Id { get; set; }
}

/// <summary>DELETE /roles/{id} — 刪除（軟刪除）角色；System 類與已被指派的角色不可刪（US4, FR-011/012/013）。</summary>
public sealed class DeleteRoleEndpoint(IMediator mediator)
    : Endpoint<DeleteRoleRequest, Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Delete("/roles/{id:guid}");
        // 012：授權以「角色權限維護」選單（rolePermission）為準，不沿用 /admin/roles（FR-014，設計決策 R8）。
        Policies("MenuAccess:rolePermission");
        Summary(s =>
        {
            s.Summary = "刪除角色";
            s.Responses[204] = "刪除成功";
            s.Responses[404] = "找不到角色";
            s.Responses[400] = "系統管理員類角色不可刪除／角色已被指派無法刪除";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Roles");
    }

    public override async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(DeleteRoleRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteRoleCommand(req.Id), ct);

        return result.ToDeleteResult();
    }
}
