using CsCheck;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.UserAggregate;

public class UserAccountTests
{
    // 情境：帳號正規化為 trim + 小寫（FR-012）。
    // @SPEC-004 @AC-11
    [Fact(DisplayName = "帳號正規化為去空白加小寫（FR-012）")]
    public void Normalizes_to_trimmed_lowercase()
    {
        Assert.Equal("admin", UserAccount.From("  ADMIN  ").Value);
    }

    // 情境：正規化冪等——正規化兩次與一次結果相同，且皆為小寫（property-based，FR-012）。
    // @SPEC-004 @AC-11
    [Fact(DisplayName = "帳號正規化冪等：兩次與一次結果相同且皆小寫（FR-012）")]
    public void Normalization_is_idempotent()
    {
        Gen.String[Gen.Char.AlphaNumeric, 1, 30].Sample(s =>
        {
            var once = UserAccount.From(s).Value;
            var twice = UserAccount.From(once).Value;
            return once == twice && once == once.ToLowerInvariant();
        });
    }

    // 情境：空白或超長帳號應被拒（驗證）。
    // @SPEC-004 @AC-12
    [Fact(DisplayName = "空白或超長帳號應被拒（驗證）")]
    public void Rejects_blank_or_too_long()
    {
        Assert.Throws<Vogen.ValueObjectValidationException>(() => UserAccount.From("   "));
        Assert.Throws<Vogen.ValueObjectValidationException>(() => UserAccount.From(new string('a', 51)));
    }
}
