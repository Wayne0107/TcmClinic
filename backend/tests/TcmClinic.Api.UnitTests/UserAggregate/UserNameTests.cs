using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.UserAggregate;

public class UserNameTests
{
    // 情境：顯示名稱去除前後空白。
    [Fact(DisplayName = "顯示名稱去除前後空白")]
    public void Trims_display_name()
    {
        Assert.Equal("王先生", UserName.From("  王先生 ").Value);
    }

    // 情境：空白或超過 100 字元的名稱應被拒（驗證邊界）。
    // @SPEC-004 @AC-12
    [Fact(DisplayName = "空白或超過 100 字元的名稱應被拒（邊界驗證）")]
    public void Rejects_blank_or_too_long()
    {
        Assert.Throws<Vogen.ValueObjectValidationException>(() => UserName.From("  "));
        Assert.Throws<Vogen.ValueObjectValidationException>(() => UserName.From(new string('x', 101)));
    }
}
