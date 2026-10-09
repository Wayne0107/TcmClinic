using FluentValidation;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.Web.Extensions;

/// <summary>
/// 將全系統密碼規則（<see cref="PasswordPolicy"/>）接到 FluentValidation（SPEC-013，FR-008a）。
/// 新增使用者、重設密碼、本人變更密碼三個入口共用，確保同一密碼得到相同的拒絕訊息（SC-003a）。
/// </summary>
public static class PasswordPolicyValidatorExtensions
{
    /// <summary>對密碼逐項套用 <see cref="PasswordPolicy.Validate"/>，每個未符合項目回報一筆錯誤。</summary>
    public static IRuleBuilderOptionsConditions<T, string> MustSatisfyPasswordPolicy<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.Custom((password, context) =>
        {
            foreach (var message in PasswordPolicy.Validate(password ?? string.Empty))
            {
                context.AddFailure(message);
            }
        });
}
