using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.UseCases.Profile;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Profile;

/// <summary>
/// 修改本人個人資料請求；對象一律為登入者本人，夾帶的 id／account 等識別欄位會被忽略。任一欄位不合法即整筆拒絕。
/// </summary>
/// <remarks>SPEC-013 API 契約 §2；刻意不含使用者識別欄位、不繫結用戶端夾帶的識別（FR-002）。</remarks>
public sealed class UpdateMyProfileRequest
{
    /// <summary>使用者名稱，必填；去除前後空白後 1～100 字。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>聯絡電話，選填，最長 20 字；僅可含數字、空白與 + - ( ) #（# 表分機）。null 或空白表示清除。</summary>
    public string? Phone { get; set; }

    /// <summary>電子郵件，選填，最長 100 字，須為一般 email 格式。null 或空白表示清除。</summary>
    public string? Email { get; set; }

    /// <summary>目前密碼；有填新密碼時必填且須正確（錯誤會計入登入失敗次數，達上限暫時鎖定），未填新密碼時忽略。</summary>
    public string? CurrentPassword { get; set; }

    /// <summary>新密碼，選填；null 或空字串表示不變更密碼（僅含空白視為有填寫）。填寫時須 12～128 碼、同時含大寫、小寫、數字，且不可與目前密碼相同。</summary>
    public string? NewPassword { get; set; }

    /// <summary>新密碼確認；有填新密碼時必填且須與新密碼相同；未填新密碼卻填寫此欄會被拒絕。</summary>
    public string? ConfirmNewPassword { get; set; }
}

/// <summary>修改本人個人資料成功回應。</summary>
/// <param name="Message">可直接顯示給使用者的結果訊息，格式為「個人資料維護 (帳號) [名稱] 修改成功」。</param>
/// <remarks>訊息格式依 FR-016。</remarks>
public sealed record UpdateMyProfileResponse(string Message);

/// <summary>
/// PUT /profile 格式驗證（FR-004～FR-006、FR-008、FR-009）。規則與值物件一致；密碼是否提供一律以
/// <see cref="string.IsNullOrEmpty(string?)"/> 判斷（全空白視為有填寫，設計決策 R3），不可用 NotEmpty。
/// </summary>
public sealed class UpdateMyProfileValidator : Validator<UpdateMyProfileRequest>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.DisplayName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("使用者名稱不可為空白。")
            .Must(name => name is null || name.Trim().Length <= 100)
            .WithMessage("使用者名稱長度須介於 1 至 100 字元。");

        RuleFor(x => x.Phone)
            .Must(phone => PhoneNumber.TryFrom(phone!, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("聯絡電話僅可包含數字、空白與 + - ( ) #，且不超過 20 字元。");

        RuleFor(x => x.Email)
            .Must(email => EmailAddress.TryFrom(email!, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("電子郵件格式不正確或超過 100 字元。");

        // 有填新密碼：套用全系統密碼規則，並要求新密碼確認一致與目前密碼（FR-008、FR-009）。
        When(x => !string.IsNullOrEmpty(x.NewPassword), () =>
        {
            RuleFor(x => x.NewPassword!).MustSatisfyPasswordPolicy();

            RuleFor(x => x.ConfirmNewPassword)
                .Must(confirm => !string.IsNullOrEmpty(confirm))
                .WithMessage("新密碼確認不可為空白。")
                .DependentRules(() =>
                    RuleFor(x => x.ConfirmNewPassword)
                        .Equal(x => x.NewPassword)
                        .WithMessage("新密碼與新密碼確認內容不一致。"));

            RuleFor(x => x.CurrentPassword)
                .Must(current => !string.IsNullOrEmpty(current))
                .WithMessage("目前密碼不可為空白。");
        });

        // 只填新密碼確認而未填新密碼（US3/AC3）。
        RuleFor(x => x.NewPassword)
            .Must(newPassword => !string.IsNullOrEmpty(newPassword))
            .When(x => string.IsNullOrEmpty(x.NewPassword) && !string.IsNullOrEmpty(x.ConfirmNewPassword))
            .WithMessage("新密碼不可為空白。");
    }
}

/// <summary>
/// PUT /profile — 修改登入者本人的個人資料，並可選擇性變更密碼（SPEC-013，US2、US3）。
/// 路由與請求皆不含使用者識別：對象一律為登入者本人。受「個人資料維護」選單授權控管。
/// </summary>
public sealed class UpdateMyProfileEndpoint(IMediator mediator)
    : Endpoint<UpdateMyProfileRequest, Results<Ok<UpdateMyProfileResponse>, NotFound, ValidationProblem, ProblemHttpResult>>
{
    public override void Configure()
    {
        Put("/profile");
        Policies("MenuAccess:personalData");
        Summary(s =>
        {
            s.Summary = "修改本人個人資料";
            s.Description = "修改使用者名稱、聯絡電話、電子郵件；新密碼留空表示不變更密碼，填寫時須同時提供新密碼確認與目前密碼。" +
                            "任一欄位不合法即整筆拒絕。";
            s.ExampleRequest = new UpdateMyProfileRequest
            {
                DisplayName = "everyone",
                Phone = "02-2345-6789#123",
                Email = "everyone@example.com",
            };
            s.ResponseExamples[200] = new UpdateMyProfileResponse("個人資料維護 (everyone) [everyone] 修改成功");
            s.Responses[200] = "修改成功";
            s.Responses[400] = "欄位格式錯誤、密碼不符規則、目前密碼錯誤或嘗試次數過多";
            s.Responses[401] = "未登入";
            s.Responses[403] = "無「個人資料維護」選單權限";
            s.Responses[404] = "登入者已不存在（正常情況由每請求重驗先回 401）";
            s.Responses[500] = "非預期錯誤：回傳 ProblemDetails，detail 為「個人資料維護 (帳號) [名稱] 修改失敗」";
        });
        Tags("Profile");
    }

    public override async Task<Results<Ok<UpdateMyProfileResponse>, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(UpdateMyProfileRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateMyProfileCommand(
                req.DisplayName, req.Phone, req.Email, req.CurrentPassword, req.NewPassword, req.ConfirmNewPassword),
            ct);

        return result.ToUpdateResult(message => new UpdateMyProfileResponse(message));
    }
}
