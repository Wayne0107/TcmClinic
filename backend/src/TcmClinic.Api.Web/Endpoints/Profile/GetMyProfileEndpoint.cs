using System.Globalization;
using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Profile;
using TcmClinic.Api.Web.Extensions;

namespace TcmClinic.Api.Web.Endpoints.Profile;

/// <summary>登入者所屬的一個角色。</summary>
/// <param name="Code">角色代碼（例如 SUPERADMIN）。</param>
/// <param name="Name">角色名稱（例如 超級管理員）。</param>
public sealed record MyProfileRoleResponse(string Code, string Name);

/// <summary>登入者本人的個人資料；不含任何密碼資訊。</summary>
/// <param name="Account">登入帳號（唯讀，不可修改）。</param>
/// <param name="DisplayName">使用者名稱。</param>
/// <param name="Roles">所屬角色清單，依角色名稱排序；無角色時為空陣列。</param>
/// <param name="Phone">聯絡電話；未設定時為 null。</param>
/// <param name="Email">電子郵件；未設定時為 null。</param>
/// <param name="PasswordChangedAt">上次修改密碼日期，格式 yyyy-MM-dd；從未修改過時為帳號建立日。</param>
/// <param name="PasswordRemainingDays">密碼尚餘有效天數（單位：天），介於 0 與密碼效期天數（預設 90）之間；修改當日算第 1 天。</param>
/// <param name="PasswordExpired">密碼是否已逾期（剩餘天數為 0 時為 true）；僅供顯示，不限制使用。</param>
/// <param name="Status">帳號狀態：Enabled（啟用）或 Disabled（停用）；停用機制上線前恆為 Enabled。</param>
/// <remarks>SPEC-013 API 契約 §1。</remarks>
public sealed record MyProfileResponse(
    string Account,
    string DisplayName,
    IReadOnlyList<MyProfileRoleResponse> Roles,
    string? Phone,
    string? Email,
    string PasswordChangedAt,
    int PasswordRemainingDays,
    bool PasswordExpired,
    string Status);

/// <summary>
/// GET /profile — 查詢登入者本人的個人資料（SPEC-013，US1、US4，FR-001/002/011/013/014）。
/// 路由不含任何使用者識別：對象一律為登入者本人。受「個人資料維護」選單授權控管。
/// </summary>
public sealed class GetMyProfileEndpoint(IMediator mediator)
    : EndpointWithoutRequest<Results<Ok<MyProfileResponse>, NotFound, ProblemHttpResult>>
{
    public override void Configure()
    {
        Get("/profile");
        Policies("MenuAccess:personalData");
        Summary(s =>
        {
            s.Summary = "查詢本人個人資料";
            s.Description = "回傳登入者本人的帳號、名稱、角色、聯絡資料、上次修改密碼日期、密碼剩餘有效天數與帳號狀態；不含任何密碼資訊。";
            s.ResponseExamples[200] = new MyProfileResponse(
                "everyone", "everyone", [new MyProfileRoleResponse("SUPERADMIN", "超級管理員")],
                null, null, "2026-08-27", 59, false, "Enabled");
            s.Responses[200] = "本人個人資料";
            s.Responses[401] = "未登入";
            s.Responses[403] = "無「個人資料維護」選單權限";
            s.Responses[404] = "登入者已不存在（正常情況由每請求重驗先回 401）";
        });
        Tags("Profile");
    }

    public override async Task<Results<Ok<MyProfileResponse>, NotFound, ProblemHttpResult>>
        ExecuteAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyProfileQuery(), ct);

        return result.ToGetByIdResult(dto => new MyProfileResponse(
            dto.Account,
            dto.DisplayName,
            dto.Roles.Select(r => new MyProfileRoleResponse(r.Code, r.Name)).ToList(),
            dto.Phone,
            dto.Email,
            dto.PasswordChangedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            dto.PasswordRemainingDays,
            dto.PasswordExpired,
            dto.Status));
    }
}
