using Vogen;

namespace TcmClinic.Api.Core.MenuAggregate;

[ValueObject<string>]
public readonly partial struct MenuItemName
{
    private static Validation Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 30
            ? Validation.Ok
            : Validation.Invalid("選單名稱長度須介於 1 至 30 字元。");

    private static string NormalizeInput(string input) => input.Trim();
}
