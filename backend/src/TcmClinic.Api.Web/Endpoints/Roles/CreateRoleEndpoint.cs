using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Roles;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Roles;

/// <summary>新增角色請求。</summary>
/// <remarks>
/// 012：<see cref="Code"/> 選填——省略則由後端自動產生 R###；<see cref="RoleType"/>
/// 必填且僅接受 Custom（System 僅來自種子，FR-004）。
/// </remarks>
/// <param name="Code">角色代碼，選填，最長 50 字；提供時須於未刪除角色中唯一，省略或空白則由後端自動產生「R」加三位數序號（如 R009）。</param>
/// <param name="Name">角色名稱，必填，最長 20 字。</param>
/// <param name="Description">角色說明，選填，最長 200 字。</param>
/// <param name="RoleType">角色種類，必填；新增僅接受 <c>Custom</c>（一般使用者類），送 <c>System</c> 或其他值會被拒絕。</param>
public sealed record CreateRoleRequest(string? Code, string Name, string? Description, string RoleType);

/// <summary>新增角色回應。</summary>
/// <param name="Id">新建角色的對外識別碼（UUIDv7）。</param>
public sealed record CreateRoleResponse(Guid Id);

/// <summary>
/// 新增角色請求驗證（012，US2/AC3、FR-005）：長度上限對齊 <c>RoleName</c>／<c>RoleCode</c> 值物件與 SysRole 欄位，
/// 使缺必填或超長回 400 並指出欄位，而非於值物件建構時擲例外成 500。角色種類合法值由用例檢查。
/// </summary>
public sealed class CreateRoleValidator : Validator<CreateRoleRequest>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(20).WithMessage("角色名稱為必填，且不得超過 20 字元。");
        RuleFor(x => x.RoleType).NotEmpty().WithMessage("角色種類為必填（僅接受 Custom）。");
        RuleFor(x => x.Description).MaximumLength(200).WithMessage("角色說明不得超過 200 字元。");
        RuleFor(x => x.Code).MaximumLength(50).WithMessage("角色代碼不得超過 50 字元。");
    }
}

/// <summary>POST /roles — 新增角色（US4/US2, FR-005/006/007）。</summary>
public sealed class CreateRoleEndpoint(IMediator mediator)
    : Endpoint<CreateRoleRequest, Results<Created<CreateRoleResponse>, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Post("/roles");
        // 012：授權以「角色權限維護」選單（rolePermission）為準，不沿用 /admin/roles（FR-014，設計決策 R8）。
        Policies("MenuAccess:rolePermission");
        Summary(s =>
        {
            s.Summary = "新增角色";
            s.ExampleRequest = new CreateRoleRequest(null, "編輯者", "可編輯內容的角色", "Custom");
            s.ResponseExamples[201] = new CreateRoleResponse(ExampleIds.Role2);
            s.Responses[201] = "建立成功";
            s.Responses[400] = "缺必填／欄位超長／角色代碼重複／角色種類非 Custom";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Roles");
    }

    public override async Task<Results<Created<CreateRoleResponse>, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(CreateRoleRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateRoleCommand(req.Code, req.Name, req.Description, req.RoleType), ct);

        return result.ToCreatedResult(id => $"/roles/{id}", id => new CreateRoleResponse(id));
    }
}
