using System.ComponentModel.DataAnnotations;

namespace TcmClinic.Api.UseCases.Profile;

/// <summary>
/// 密碼效期設定（SPEC-013，FR-011）。設定區段 <c>PasswordPolicy</c>，由 Web 層綁定並以 singleton 注入，
/// 用例層不依賴 Options 框架。日後「系統參數維護」上線時改由其提供。
/// </summary>
public sealed class PasswordPolicyOptions
{
    /// <summary>設定區段名稱。</summary>
    public const string SectionName = "PasswordPolicy";

    /// <summary>密碼有效天數（預設 90，對齊舊系統 system.change.password.time）；須介於 1 至 3650，啟動時驗證。</summary>
    [Range(1, 3650)]
    public int ValidDays { get; set; } = 90;
}
