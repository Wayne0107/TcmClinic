using Vogen;

namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>使用者名稱（顯示用）。</summary>
[ValueObject<string>]
public readonly partial struct UserName
{
    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 100
            ? Validation.Ok
            : Validation.Invalid("使用者名稱長度須介於 1 至 100 字元。");

    private static string NormalizeInput(string input) => input.Trim();
}
