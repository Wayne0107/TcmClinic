using CsCheck;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.UserAggregate;

/// <summary>聯絡電話值物件（SPEC-013，FR-005）：字元集與長度規則。</summary>
public class PhoneNumberTests
{
    private const string AllowedNonSpace = "0123456789+-()#";

    // 情境：僅由允許字元（不含首尾空白）組成、長度 1～20 的字串一律被接受且值不變（不變式）。
    [Fact(DisplayName = "允許字元組成且長度 1～20 的電話一律被接受（FR-005）")]
    public void Accepts_all_allowed_strings_within_length()
    {
        Gen.String[Gen.Char[AllowedNonSpace], 1, 20].Sample(s => PhoneNumber.From(s).Value == s);
    }

    // 情境：中間含空白（如分段書寫）亦為合法電話。
    [Fact(DisplayName = "中間含空白的電話被接受（FR-005）")]
    public void Accepts_inner_spaces()
    {
        Assert.Equal("02 2345 6789#123", PhoneNumber.From("02 2345 6789#123").Value);
    }

    // 情境：含任何英文字母的字串一律被拒（不變式）。
    // @SPEC-013 @AC-7
    [Fact(DisplayName = "含英文字母的電話一律被拒（FR-005）")]
    public void Rejects_strings_containing_letters()
    {
        Gen.Select(Gen.String[Gen.Char[AllowedNonSpace], 0, 10], Gen.Char['a', 'z'])
            .Sample((prefix, letter) =>
                !PhoneNumber.TryFrom(prefix + letter, out _));
    }

    // 情境：長度 21 以上的數字字串一律被拒（長度上限不變式）。
    [Fact(DisplayName = "長度超過 20 的電話一律被拒（FR-005）")]
    public void Rejects_strings_longer_than_20()
    {
        Gen.String[Gen.Char["0123456789"], 21, 60].Sample(s => !PhoneNumber.TryFrom(s, out _));
    }

    // 情境：前後空白被去除；全空白視為無效（清除由用例以 null 表達）。
    [Fact(DisplayName = "電話去除前後空白，全空白被拒（FR-005）")]
    public void Trims_and_rejects_blank()
    {
        Assert.Equal("0912345678", PhoneNumber.From("  0912345678 ").Value);
        Assert.Throws<Vogen.ValueObjectValidationException>(() => PhoneNumber.From("   "));
    }
}
