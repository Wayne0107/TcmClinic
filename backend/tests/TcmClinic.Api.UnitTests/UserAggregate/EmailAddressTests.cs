using CsCheck;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.UserAggregate;

/// <summary>電子郵件值物件（SPEC-013，FR-006）：一般格式與長度規則。</summary>
public class EmailAddressTests
{
    // 情境：一般格式的電子郵件被接受，前後空白去除、大小寫保留。
    [Fact(DisplayName = "一般格式的電子郵件被接受並去除前後空白（FR-006）")]
    public void Accepts_valid_and_trims()
    {
        Assert.Equal("a@b.co", EmailAddress.From("a@b.co").Value);
        Assert.Equal("Everyone@Example.com", EmailAddress.From("  Everyone@Example.com ").Value);
    }

    // 情境：各種格式錯誤皆被拒（無 @、多個 @、local/domain 為空、domain 無點、含空白）。
    // @SPEC-013 @AC-7
    [Theory(DisplayName = "格式錯誤的電子郵件被拒（FR-006）")]
    [InlineData("abc")]
    [InlineData("a@@b.co")]
    [InlineData("a@b@c.co")]
    [InlineData("@b.co")]
    [InlineData("a@")]
    [InlineData("a@bco")]
    [InlineData("a b@c.co")]
    [InlineData("   ")]
    public void Rejects_malformed(string value)
    {
        Assert.False(EmailAddress.TryFrom(value, out _));
    }

    // 情境：總長 101 以上一律被拒（長度上限不變式）。
    [Fact(DisplayName = "長度超過 100 的電子郵件一律被拒（FR-006）")]
    public void Rejects_longer_than_100()
    {
        Gen.String[Gen.Char["abcdefghijklmnopqrstuvwxyz0123456789"], 96, 200]
            .Sample(local => !EmailAddress.TryFrom(local + "@b.co", out _));
    }

    // 情境：總長恰為 100 的合法電子郵件被接受（邊界）。
    [Fact(DisplayName = "長度恰為 100 的電子郵件被接受（邊界，FR-006）")]
    public void Accepts_exactly_100()
    {
        var value = new string('a', 95) + "@b.co";
        Assert.Equal(100, value.Length);
        Assert.True(EmailAddress.TryFrom(value, out _));
    }
}
