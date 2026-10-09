using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Roles;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Roles;

/// <summary>
/// GET /roles/{id}/menu-visibility — 取得某角色的可見選單設定（US2/US3, FR-008/015）。
/// 012：於 <c>ETag</c> 回應標頭附角色並行權杖（base64 rowVersion），供 PUT 設定權限時必填回帶。
/// </summary>
public sealed class GetRoleVisibilityEndpoint(IMediator mediator)
    : EndpointWithoutRequest<IReadOnlyList<RoleMenuVisibilityNodeDto>>
{
    public override void Configure()
    {
        Get("/roles/{id:guid}/menu-visibility");
        // 012：授權以「角色權限維護」選單（rolePermission）為準，不沿用 /admin/roles（FR-014，設計決策 R8）。
        Policies("MenuAccess:rolePermission");
        Summary(s =>
        {
            s.Summary = "取得角色可見選單";
            s.Params["id"] = "角色對外識別碼（UUIDv7）。";
            s.ResponseExamples[200] = new List<RoleMenuVisibilityNodeDto>
            {
                new(ExampleIds.Menu, "系統管理作業", "system", false,
                    [new(ExampleIds.Menu2, "帳號維護", "accountMaintenance", true, [])]),
            };
            s.Responses[200] = "以選單樹呈現，標示各子項是否已授權；ETag 標頭含並行權杖";
            s.Responses[404] = "找不到角色";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Roles");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await mediator.Send(new GetRoleMenuVisibilityQuery(id), ct);

        if (result.Status == Ardalis.Result.ResultStatus.NotFound)
        {
            // 空 body 的 404 交由全域 StatusCodePages 補上 ProblemDetails（ProblemDetailsConfigs）；
            // Send.NotFoundAsync 會直接送出回應，StatusCodePages 無法補上。
            await Send.ResultAsync(TypedResults.NotFound());
            return;
        }

        // 並行權杖以 ETag 標頭傳遞（不改動既有 body 陣列形狀，維持相容）。
        HttpContext.Response.Headers.ETag = $"\"{result.Value.RowVersion}\"";
        await Send.OkAsync(result.Value.Nodes, ct);
    }
}
