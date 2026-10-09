using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Roles;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Roles;

/// <summary>設定角色可見選單請求。</summary>
public sealed class SetRoleVisibilityRequest
{
    /// <summary>角色對外識別碼（UUIDv7）（路由參數 {id}）。</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 要授權的選單項目對外識別碼（UUIDv7）清單，整份覆寫該角色現有授權；只能是葉節點（無子節點）選單，含容器或不存在的 ID 時整筆拒絕。空清單表示清除全部授權。
    /// </summary>
    public List<Guid> GrantedMenuItemIds { get; set; } = [];

    /// <summary>並行權杖（必填）：由 GET menu-visibility 的 ETag 或維護清單取得，儲存時原樣回帶。</summary>
    /// <remarks>SPEC-012 並行控制。</remarks>
    public string? RowVersion { get; set; }
}

/// <summary>PUT /roles/{id}/menu-visibility — 設定某角色的可見選單（US2/US3, FR-009/010/011/015）。</summary>
public sealed class SetRoleVisibilityEndpoint(IMediator mediator)
    : Endpoint<SetRoleVisibilityRequest, Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Put("/roles/{id:guid}/menu-visibility");
        // 012：授權以「角色權限維護」選單（rolePermission）為準，不沿用 /admin/roles（FR-014，設計決策 R8）。
        Policies("MenuAccess:rolePermission");
        Summary(s =>
        {
            s.Summary = "設定角色可見選單";
            s.Description = "以「被授權的有路由子項」清單覆寫；容器 id 會被拒（FR-010）。需帶 rowVersion 做並行控制（FR-015）。";
            s.ExampleRequest = new SetRoleVisibilityRequest
            {
                Id = ExampleIds.Role2,
                GrantedMenuItemIds = [ExampleIds.Menu2, ExampleIds.Menu3],
                RowVersion = "AAAAAAAAB9E=",
            };
            s.Responses[200] = "更新成功";
            s.Responses[404] = "找不到角色";
            s.Responses[400] = "含容器或不存在的 id／缺 rowVersion";
            s.Responses[409] = "rowVersion 不符（他人已先變更）";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Roles");
    }

    public override async Task<Results<Ok, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(SetRoleVisibilityRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new SetRoleMenuVisibilityCommand(req.Id, req.GrantedMenuItemIds, req.RowVersion), ct);

        return result.ToUpdateResult();
    }
}
