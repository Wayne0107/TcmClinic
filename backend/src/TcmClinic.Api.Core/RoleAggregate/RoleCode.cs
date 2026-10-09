using Vogen;

namespace TcmClinic.Api.Core.RoleAggregate;

[ValueObject<string>]
public readonly partial struct RoleCode
{
    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 50
            ? Validation.Ok
            : Validation.Invalid("RoleCode 長度須介於 1 至 50 字元。");

    private static string NormalizeInput(string input) => input.Trim();
}
