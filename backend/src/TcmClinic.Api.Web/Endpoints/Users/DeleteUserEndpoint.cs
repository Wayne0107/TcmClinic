using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Users;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Users;

/// <summary>DELETE /users/{id} — 軟刪除使用者（US5，FR-017/019）。含防鎖死檢查。</summary>
public sealed class DeleteUserEndpoint(IMediator mediator)
    : EndpointWithoutRequest<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Delete("/users/{id:guid}");
        // 以「帳號維護」選單（accountMaintenance）授權（SPEC-005 選單導向授權）。
        Policies("MenuAccess:accountMaintenance");
        Summary(s =>
        {
            s.Summary = "刪除使用者（軟刪除）";
            s.Params["id"] = "要刪除的使用者對外識別碼（UUIDv7）。";
            s.Responses[204] = "刪除成功";
            s.Responses[400] = "防鎖死攔截（最後超管／刪除自己）";
            s.Responses[404] = "找不到使用者";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Users");
    }

    public override async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await mediator.Send(new DeleteUserCommand(id), ct);

        return result.ToDeleteResult();
    }
}
