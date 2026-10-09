using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Users;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Users;

/// <summary>新增使用者請求。</summary>
/// <param name="Account">登入帳號，最長 50 字；未刪除範圍內唯一（不分大小寫）。</param>
/// <param name="DisplayName">顯示名稱，最長 100 字。</param>
/// <param name="Password">密碼，12～128 碼，須同時含大寫、小寫、數字。</param>
/// <param name="RoleIds">指派的角色對外識別碼（UUIDv7） 清單，至少一個。</param>
public sealed record CreateUserRequest(
    string Account, string DisplayName, string Password, List<Guid> RoleIds);

/// <summary>新增使用者回應。</summary>
/// <param name="Id">新建使用者的對外識別碼（UUIDv7）。</param>
public sealed record CreateUserResponse(Guid Id);

public sealed class CreateUserValidator : Validator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Account).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MustSatisfyPasswordPolicy(); // 全系統統一密碼規則（013 FR-008a，改 FR-013）
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("須指派至少一個角色。");
    }
}

/// <summary>POST /users — 新增使用者（US3，FR-011/012/013）。</summary>
public sealed class CreateUserEndpoint(IMediator mediator)
    : Endpoint<CreateUserRequest, Results<Created<CreateUserResponse>, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Post("/users");
        // 以「帳號維護」選單（accountMaintenance）授權（SPEC-005 選單導向授權）。
        Policies("MenuAccess:accountMaintenance");
        Summary(s =>
        {
            s.Summary = "新增使用者";
            s.Description = "帳號未刪除範圍內唯一（不分大小寫）；密碼須 12～128 碼且同時含大寫、小寫、數字；須指派至少一個角色。";
            s.ExampleRequest = new CreateUserRequest("jdoe", "John Doe", "P@ssw0rd1234", [ExampleIds.Role2]);
            s.ResponseExamples[201] = new CreateUserResponse(ExampleIds.User);
            s.Responses[201] = "建立成功";
            s.Responses[400] = "驗證失敗或帳號已存在";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Users");
    }

    public override async Task<Results<Created<CreateUserResponse>, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(CreateUserRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateUserCommand(req.Account, req.DisplayName, req.Password, req.RoleIds), ct);

        return result.ToCreatedResult(id => $"/users/{id}", id => new CreateUserResponse(id));
    }
}
