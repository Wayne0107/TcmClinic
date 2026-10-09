using System.Text;
using Ardalis.Result;
using Mediator;
using Microsoft.Extensions.Logging;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Auth;

namespace TcmClinic.Api.UseCases.Profile;

/// <summary>
/// 修改登入者本人的個人資料，並可選擇性變更密碼（SPEC-013，US2、US3，FR-002～FR-010、FR-015、FR-016）。
/// 對象一律取自登入身分；命令不含任何使用者識別（防 IDOR）。<see cref="NewPassword"/> 為 null 或空字串表示不變更密碼。
/// </summary>
public sealed record UpdateMyProfileCommand(
    string DisplayName,
    string? Phone,
    string? Email,
    string? CurrentPassword,
    string? NewPassword,
    string? ConfirmNewPassword) : ICommand<Result<string>>
{
    /// <summary>字串表示遮蔽三個密碼欄位，避免被記錄或除錯輸出時外洩（SC-006）。</summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"DisplayName = {DisplayName}, Phone = {Phone}, Email = {Email}, ");
        builder.Append($"HasNewPassword = {!string.IsNullOrEmpty(NewPassword)}");
        return true;
    }
}

public sealed class UpdateMyProfileHandler(
    ICurrentUser currentUser,
    IRepository<User> userRepository,
    IPasswordHasher passwordHasher,
    ILoginThrottle loginThrottle,
    IUserLogRecorder userLog,
    TimeProvider timeProvider,
    ILogger<UpdateMyProfileHandler> logger)
    : ICommandHandler<UpdateMyProfileCommand, Result<string>>
{
    /// <summary>操作紀錄的功能代碼與動作（對齊舊系統 USER_LOG）。</summary>
    public const string FunctionId = "PersonalData";

    public const string ModifyAction = "Modify";

    public async ValueTask<Result<string>> Handle(
        UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        if (!UserAccount.TryFrom(currentUser.UserName, out var account))
        {
            return Result<string>.NotFound();
        }

        var user = await userRepository.FirstOrDefaultAsync(
            new UserByAccountForUpdateSpec(account), cancellationToken);
        if (user is null)
        {
            return Result<string>.NotFound();
        }

        // 所有檢查通過後才變更聚合（FR-007、設計決策 R9：不得部分成功）。
        var displayName = UserName.From(command.DisplayName);
        PhoneNumber? phone = string.IsNullOrWhiteSpace(command.Phone) ? null : PhoneNumber.From(command.Phone);
        EmailAddress? email = string.IsNullOrWhiteSpace(command.Email) ? null : EmailAddress.From(command.Email);

        var changePassword = !string.IsNullOrEmpty(command.NewPassword);
        if (changePassword)
        {
            var rejection = await CheckPasswordChangeAsync(command, user, account, cancellationToken);
            if (rejection is not null)
            {
                return rejection;
            }
        }

        user.Rename(displayName).UpdateContact(phone, email);
        if (changePassword)
        {
            var (hash, salt) = passwordHasher.Hash(command.NewPassword!);
            user.SetPassword(hash, salt, timeProvider.GetLocalNow().DateTime);
        }

        try
        {
            await userRepository.UpdateAsync(user, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 非預期錯誤（spec Edge Case、FR-015、FR-016）：保留例外於日誌供診斷、寫入失敗操作紀錄，
            // 並回傳可直接顯示的「……修改失敗」（CriticalError → 500 ProblemDetails，不外洩例外細節）。
            var failure = FailureMessage(account, displayName, null);
            logger.LogError(ex, "[PERSONAL_DATA] 個人資料修改儲存失敗：Account={Account}", account.Value);
            await RecordAsync(UserLogStatus.Failure, failure, cancellationToken);
            return Result<string>.CriticalError(failure);
        }

        if (changePassword)
        {
            // 變更成功即清除失敗計數（對應舊系統變更密碼時清除登入錯誤次數，設計決策 R4）。
            loginThrottle.Reset(account.Value);
        }

        var message = $"個人資料維護 ({account.Value}) [{displayName.Value}] 修改成功";
        await RecordAsync(UserLogStatus.Success, message, cancellationToken);
        return Result<string>.Success(message);
    }

    /// <summary>
    /// 變更密碼前的業務檢查（FR-008、FR-009、設計決策 R4），全部在變更聚合前完成。
    /// 通過回傳 null；否則回傳 Invalid 並已寫入失敗紀錄。目前密碼錯誤計入與登入共用的失敗鎖定。
    /// </summary>
    private async Task<Result<string>?> CheckPasswordChangeAsync(
        UpdateMyProfileCommand command, User user, UserAccount account, CancellationToken cancellationToken)
    {
        if (loginThrottle.IsLocked(account.Value))
        {
            return await RejectAsync(user, account, "currentPassword", "嘗試次數過多，請稍後再試。", cancellationToken);
        }

        if (!passwordHasher.Verify(command.CurrentPassword ?? string.Empty, user.PasswordHash, user.PasswordSalt))
        {
            loginThrottle.RegisterFailure(account.Value);
            return await RejectAsync(user, account, "currentPassword", "目前密碼不正確。", cancellationToken);
        }

        // 防禦性檢查：用例脫離 HTTP 情境仍須守住全系統密碼規則（分層驗證）。
        var policyErrors = PasswordPolicy.Validate(command.NewPassword!);
        if (policyErrors.Count > 0)
        {
            return await RejectAsync(user, account, "newPassword", policyErrors, cancellationToken);
        }

        if (passwordHasher.Verify(command.NewPassword!, user.PasswordHash, user.PasswordSalt))
        {
            return await RejectAsync(user, account, "newPassword", "密碼不可與上次相同。", cancellationToken);
        }

        return null;
    }

    private Task<Result<string>> RejectAsync(
        User user, UserAccount account, string field, string message, CancellationToken cancellationToken) =>
        RejectAsync(user, account, field, [message], cancellationToken);

    private async Task<Result<string>> RejectAsync(
        User user, UserAccount account, string field, IReadOnlyList<string> messages, CancellationToken cancellationToken)
    {
        // 失敗原因僅為規則說明文字，不含任何密碼（FR-015）。
        await RecordAsync(
            UserLogStatus.Failure,
            FailureMessage(account, user.DisplayName, string.Join(" ", messages)),
            cancellationToken);

        return Result<string>.Invalid(messages
            .Select(m => new ValidationError { Identifier = field, ErrorMessage = m })
            .ToList());
    }

    /// <summary>失敗訊息（FR-016）；原因不得含任何密碼。</summary>
    private static string FailureMessage(UserAccount account, UserName displayName, string? reason) =>
        $"個人資料維護 ({account.Value}) [{displayName.Value}] 修改失敗" + (reason is null ? string.Empty : $"：{reason}");

    private Task RecordAsync(UserLogStatus status, string content, CancellationToken cancellationToken) =>
        userLog.RecordAsync(new UserLogRequest(FunctionId, ModifyAction, status, content), cancellationToken);
}
