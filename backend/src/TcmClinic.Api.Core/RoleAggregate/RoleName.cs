using Vogen;

namespace TcmClinic.Api.Core.RoleAggregate;

[ValueObject<string>]
public readonly partial struct RoleName
{
    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 20
            ? Validation.Ok
            : Validation.Invalid("RoleName 長度須介於 1 至 20 字元。");

    private static string NormalizeInput(string input) => input.Trim();
}
