using Vogen;

namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>
/// 帳號（登入識別碼）。正規化為 trim + 小寫，令唯一性與登入比對天然不分大小寫（FR-012）。
/// </summary>
[ValueObject<string>]
public readonly partial struct UserAccount
{
    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 50
            ? Validation.Ok
            : Validation.Invalid("帳號長度須介於 1 至 50 字元。");

    private static string NormalizeInput(string input) =>
        input.Trim().ToLowerInvariant();
}
