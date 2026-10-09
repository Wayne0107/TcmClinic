using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Profile;

/// <summary>
/// 查詢登入者本人的個人資料（SPEC-013，US1、US4，FR-001/002/011/013）。
/// 對象一律取自已驗證的登入身分（<see cref="ICurrentUser.UserName"/>），查詢不接受任何使用者識別參數（防 IDOR）。
/// </summary>
public sealed record GetMyProfileQuery : IQuery<Result<MyProfileDto>>;

public sealed class GetMyProfileHandler(
    ICurrentUser currentUser,
    IReadRepository<User> userRepository,
    IReadRepository<Role> roleRepository,
    TimeProvider timeProvider,
    PasswordPolicyOptions passwordPolicy)
    : IQueryHandler<GetMyProfileQuery, Result<MyProfileDto>>
{
    /// <summary>
    /// 帳號狀態：SysUser 尚無啟用／停用欄位，能通過每請求重驗者即為啟用。
    /// 停用機制由「帳號維護」規格引入後改讀實際狀態（設計決策 R10）。
    /// </summary>
    private const string EnabledStatus = "Enabled";

    public async ValueTask<Result<MyProfileDto>> Handle(
        GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        if (!UserAccount.TryFrom(currentUser.UserName, out var account))
        {
            return Result<MyProfileDto>.NotFound();
        }

        var user = await userRepository.FirstOrDefaultAsync(new UserByAccountSpec(account), cancellationToken);
        if (user is null)
        {
            return Result<MyProfileDto>.NotFound();
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        var roles = (await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken))
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => new MyProfileRoleDto(r.Code.Value, r.Name.Value))
            .ToList();

        // 上線前建立的帳號無修改紀錄，以建立時間代替（FR-011、設計決策 R5）。
        var lastChanged = DateOnly.FromDateTime(user.PasswordChangedAt ?? user.CreateDate);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var remaining = PasswordExpiry.RemainingDays(lastChanged, today, passwordPolicy.ValidDays);

        return Result<MyProfileDto>.Success(new MyProfileDto(
            user.Account.Value,
            user.DisplayName.Value,
            roles,
            user.Phone?.Value,
            user.Email?.Value,
            lastChanged,
            remaining,
            PasswordExpired: remaining == 0,
            EnabledStatus));
    }
}
