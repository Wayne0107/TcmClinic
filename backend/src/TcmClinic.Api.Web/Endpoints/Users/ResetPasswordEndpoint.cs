using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Users;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Users;

/// <summary>重設使用者密碼請求。</summary>
public sealed class ResetPasswordRequest
{
    /// <summary>要重設密碼的使用者對外識別碼（UUIDv7）（取自路由 {id}）。</summary>
    public Guid Id { get; set; }

    /// <summary>新密碼，必填；12～128 碼，須同時含大寫、小寫、數字。重設後舊密碼即失效。</summary>
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ResetPasswordValidator : Validator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MustSatisfyPasswordPolicy(); // 全系統統一密碼規則（013 FR-008a，改 FR-013）
    }
}

/// <summary>PUT /users/{id}/password — 重設密碼（US4，FR-014）。舊密碼即失效。</summary>
public sealed class ResetPasswordEndpoint(IMediator mediator)
    : Endpoint<ResetPasswordRequest, Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Put("/users/{id:guid}/password");
        // 以「帳號維護」選單（accountMaintenance）授權（SPEC-005 選單導向授權）。
        Policies("MenuAccess:accountMaintenance");
        Summary(s =>
        {
            s.Summary = "重設密碼";
            s.ExampleRequest = new ResetPasswordRequest { Id = ExampleIds.User, NewPassword = "P@ssw0rd1234" };
            s.Responses[204] = "重設成功";
            s.Responses[400] = "新密碼未達強度規則（12～128 碼且同時含大寫、小寫、數字）";
            s.Responses[404] = "找不到使用者";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Users");
    }

    public override async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new ResetPasswordCommand(req.Id, req.NewPassword), ct);

        return result.ToNoContentResult();
    }
}
