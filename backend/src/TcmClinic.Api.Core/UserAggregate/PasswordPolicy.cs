namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>
/// 全系統唯一的密碼強度規則來源（SPEC-013，FR-008、FR-008a；修改 004 FR-013）：
/// 長度 12～128 碼，且同時包含英文大寫字母、英文小寫字母、數字（對齊舊系統 UserHelper.validatePWD）。
/// 新增使用者、管理者重設密碼、本人變更密碼皆經此規則；新增入口不得自行撰寫規則。
/// 既有密碼不追溯（登入時不檢查本規則）。上限 128 防止過長輸入造成資源濫用。
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;

    public const int MaxLength = 128;

    /// <summary>檢查密碼是否符合規則。</summary>
    /// <param name="password">明文密碼（僅於記憶體中檢查，不保存）。</param>
    /// <returns>所有未符合項目的訊息；空清單表示通過。</returns>
    public static IReadOnlyList<string> Validate(string password)
    {
        var errors = new List<string>();

        if (password.Length is < MinLength or > MaxLength)
        {
            errors.Add("密碼長度須介於 12 至 128 碼。");
        }

        if (!password.Any(c => c is >= 'A' and <= 'Z'))
        {
            errors.Add("密碼須包含英文大寫字母（A-Z）。");
        }

        if (!password.Any(c => c is >= 'a' and <= 'z'))
        {
            errors.Add("密碼須包含英文小寫字母（a-z）。");
        }

        if (!password.Any(c => c is >= '0' and <= '9'))
        {
            errors.Add("密碼須包含數字（0-9）。");
        }

        return errors;
    }
}
