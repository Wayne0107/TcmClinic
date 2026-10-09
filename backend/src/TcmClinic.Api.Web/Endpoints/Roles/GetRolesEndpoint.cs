using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Roles;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Roles;

/// <summary>GET /roles — 角色清單（US2/US4, FR-008）。</summary>
public sealed class GetRolesEndpoint(IMediator mediator)
    : EndpointWithoutRequest<Ok<IReadOnlyList<RoleDto>>>
{
    public override void Configure()
    {
        Get("/roles");
        // 2026-10-02：舊後台選單移除，跨頁共用改為「帳號維護」或「角色權限維護」任一可見即可。
        Policies("MenuAccess:accountMaintenance|rolePermission");
        Summary(s =>
        {
            s.Summary = "角色清單";
            s.ResponseExamples[200] = new List<RoleDto>
            {
                new(ExampleIds.Role, "SUPERADMIN", "超級管理員", "系統最高權限"),
                new(ExampleIds.Role2, "EDITOR", "編輯者", "可編輯內容的角色"),
            };
            s.Responses[200] = "角色清單";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Roles");
    }

    public override async Task<Ok<IReadOnlyList<RoleDto>>> ExecuteAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetRolesQuery(), ct);
        return result.ToOkOnlyResult(roles => roles);
    }
}
