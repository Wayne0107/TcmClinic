using Vogen;

namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>
/// 聯絡電話（SPEC-013，FR-005）。長度 1～20，僅允許數字、空白與 <c>+ - ( ) #</c>（# 表分機）。
/// </summary>
[ValueObject<string>]
public readonly partial struct PhoneNumber
{
    public const int MaxLength = 20;

    private static Validation Validate(string value) =>
        value.Length is >= 1 and <= MaxLength && value.All(IsAllowedCharacter)
            ? Validation.Ok
            : Validation.Invalid("聯絡電話僅可包含數字、空白與 + - ( ) #，且不超過 20 字元。");

    /// <summary>電話字元規則（數字、空白與 + - ( ) #）；分行電話（SPEC-014）共用此規則。</summary>
    public static bool IsAllowedCharacter(char c) =>
        c is >= '0' and <= '9' or ' ' or '+' or '-' or '(' or ')' or '#';

    private static string NormalizeInput(string input) => input.Trim();
}
