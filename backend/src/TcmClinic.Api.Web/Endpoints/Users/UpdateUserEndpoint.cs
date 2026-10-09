using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Users;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Users;

/// <summary>修改使用者請求（帳號不可修改）。</summary>
public sealed class UpdateUserRequest
{
    /// <summary>要修改的使用者對外識別碼（UUIDv7）（取自路由 {id}）。</summary>
    public Guid Id { get; set; }

    /// <summary>顯示名稱，必填，最長 100 字。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>所屬角色對外識別碼（UUIDv7） 清單，必填且至少一個，須皆為存在的角色；整批取代原有角色，重複的識別碼 視為一個。</summary>
    public List<Guid> RoleIds { get; set; } = [];
}

/// <summary>修改使用者回應。</summary>
/// <param name="Id">已修改的使用者對外識別碼（UUIDv7）。</param>
public sealed record UpdateUserResponse(Guid Id);

public sealed class UpdateUserValidator : Validator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("每位使用者至少需保留一個角色。");
    }
}

/// <summary>PUT /users/{id} — 修改使用者名稱與角色（US4，FR-014/016/019）。帳號不可改。</summary>
public sealed class UpdateUserEndpoint(IMediator mediator)
    : Endpoint<UpdateUserRequest, Results<Ok<UpdateUserResponse>, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Put("/users/{id:guid}");
        // 以「帳號維護」選單（accountMaintenance）授權（SPEC-005 選單導向授權）。
        Policies("MenuAccess:accountMaintenance");
        Summary(s =>
        {
            s.Summary = "修改使用者";
            s.Description = "修改顯示名稱與所屬角色（至少一個）；帳號不可修改。含防鎖死檢查。";
            s.ExampleRequest = new UpdateUserRequest { Id = ExampleIds.User, DisplayName = "John Doe", RoleIds = [ExampleIds.Role2] };
            s.ResponseExamples[200] = new UpdateUserResponse(ExampleIds.User);
            s.Responses[200] = "更新成功";
            s.Responses[400] = "驗證失敗或防鎖死攔截";
            s.Responses[404] = "找不到使用者";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Users");
    }

    public override async Task<Results<Ok<UpdateUserResponse>, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(UpdateUserRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateUserCommand(req.Id, req.DisplayName, req.RoleIds), ct);

        return result.ToUpdateResult(() => new UpdateUserResponse(req.Id));
    }
}
