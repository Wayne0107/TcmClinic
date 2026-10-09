using CsCheck;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.UserAggregate;

/// <summary>
/// 全系統密碼強度規則（SPEC-013，FR-008、FR-008a；改 004 FR-013）：12～128 碼，
/// 且同時包含大寫字母、小寫字母、數字。
/// </summary>
public class PasswordPolicyTests
{
    private const string LengthMessage = "密碼長度須介於 12 至 128 碼。";
    private const string UpperMessage = "密碼須包含英文大寫字母（A-Z）。";
    private const string LowerMessage = "密碼須包含英文小寫字母（a-z）。";
    private const string DigitMessage = "密碼須包含數字（0-9）。";

    private static readonly Gen<char> AnyAsciiPrintable = Gen.Char[(char)33, (char)126];

    // 情境：長度 12～128 且含大寫、小寫、數字者一律通過（不變式）。
    [Fact(DisplayName = "長度 12～128 且含大寫、小寫、數字的密碼一律通過（FR-008）")]
    public void Accepts_all_compliant_passwords()
    {
        Gen.Select(Gen.Char['A', 'Z'], Gen.Char['a', 'z'], Gen.Char['0', '9'], Gen.String[AnyAsciiPrintable, 9, 125])
            .Sample((upper, lower, digit, rest) =>
                PasswordPolicy.Validate($"{upper}{lower}{digit}{rest}").Count == 0);
    }

    // 情境：長度不在 12～128 者必含長度訊息（不變式）。
    [Fact(DisplayName = "長度不在 12～128 的密碼必回報長度訊息（FR-008）")]
    public void Rejects_out_of_range_length()
    {
        Gen.OneOf(Gen.String[AnyAsciiPrintable, 0, 11], Gen.String[AnyAsciiPrintable, 129, 300])
            .Sample(p => PasswordPolicy.Validate(p).Contains(LengthMessage));
    }

    // 情境：長度邊界——11 與 129 拒絕，12 與 128 通過。
    // @SPEC-013 @AC-12
    [Theory(DisplayName = "密碼長度邊界 11/12/128/129（FR-008）")]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Length_boundaries(int length, bool valid)
    {
        var password = "Aa1" + new string('x', length - 3);
        Assert.Equal(valid, PasswordPolicy.Validate(password).Count == 0);
    }

    // 情境：缺大寫、缺小寫、缺數字各回對應訊息（舊系統「三種組合」規則）。
    // @SPEC-013 @AC-12
    [Theory(DisplayName = "缺大寫、小寫或數字各回報對應訊息（FR-008）")]
    [InlineData("abcdefghijk1", UpperMessage)]
    [InlineData("ABCDEFGHIJK1", LowerMessage)]
    [InlineData("Abcdefghijkl", DigitMessage)]
    public void Reports_missing_character_class(string password, string expected)
    {
        Assert.Equal(new[] { expected }, PasswordPolicy.Validate(password));
    }

    // 情境：同時缺多項時回報多筆訊息（前端可一次顯示）。
    // @SPEC-013 @AC-12
    [Fact(DisplayName = "同時不符多項規則時回報多筆訊息（FR-008）")]
    public void Reports_multiple_failures()
    {
        var errors = PasswordPolicy.Validate("abc");
        Assert.Contains(LengthMessage, errors);
        Assert.Contains(UpperMessage, errors);
        Assert.Contains(DigitMessage, errors);
        Assert.DoesNotContain(LowerMessage, errors);
    }

    // 情境：全空白字串不得通過（設計決策 R3：空白視為有填寫、照規則驗證）。
    // @SPEC-013 @AC-12
    [Fact(DisplayName = "全空白密碼不通過（設計決策 R3）")]
    public void Rejects_whitespace_only()
    {
        Assert.NotEmpty(PasswordPolicy.Validate(new string(' ', 20)));
    }
}
