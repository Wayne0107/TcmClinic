using Vogen;

namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>
/// 電子郵件（SPEC-013，FR-006）。長度 1～100，一般格式：恰一個 <c>@</c>、前後段皆非空、
/// 網域含 <c>.</c> 且不以其開頭或結尾、不含空白。保留使用者輸入的大小寫。
/// </summary>
[ValueObject<string>]
public readonly partial struct EmailAddress
{
    public const int MaxLength = 100;

    private static Validation Validate(string value) =>
        value.Length is >= 1 and <= MaxLength && IsWellFormed(value)
            ? Validation.Ok
            : Validation.Invalid("電子郵件格式不正確或超過 100 字元。");

    private static bool IsWellFormed(string value)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var parts = value.Split('@');
        if (parts.Length != 2 || parts[0].Length == 0)
        {
            return false;
        }

        var domain = parts[1];
        return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
    }

    private static string NormalizeInput(string input) => input.Trim();
}
