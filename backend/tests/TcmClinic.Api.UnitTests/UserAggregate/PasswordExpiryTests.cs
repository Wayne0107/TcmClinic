using CsCheck;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.UserAggregate;

/// <summary>密碼剩餘有效天數（SPEC-013，FR-011、SC-005）：對齊舊系統 RemnantExpireDate 並保證邊界。</summary>
public class PasswordExpiryTests
{
    private static readonly DateOnly Today = new(2026, 9, 27);

    // 情境：舊系統畫面範例——效期 90 天，當日 90、31 天前 59、90 天前 0、修改日晚於今日仍為 90。
    // @SPEC-013 @AC-18
    [Theory(DisplayName = "剩餘天數對齊舊系統範例（SC-005）")]
    [InlineData(0, 90)]
    [InlineData(31, 59)]
    [InlineData(89, 1)]
    [InlineData(90, 0)]
    [InlineData(400, 0)]
    [InlineData(-5, 90)]
    public void Matches_legacy_examples(int daysAgo, int expected)
    {
        Assert.Equal(expected, PasswordExpiry.RemainingDays(Today.AddDays(-daysAgo), Today, 90));
    }

    // 情境：任意修改日與效期，結果恆介於 0 與效期之間（不變式）。
    // @SPEC-013 @AC-18
    [Fact(DisplayName = "剩餘天數恆介於 0 與效期天數之間（FR-011）")]
    public void Result_is_within_bounds()
    {
        Gen.Select(Gen.Int[-1000, 5000], Gen.Int[1, 3650]).Sample((daysAgo, validDays) =>
        {
            var remaining = PasswordExpiry.RemainingDays(Today.AddDays(-daysAgo), Today, validDays);
            return remaining >= 0 && remaining <= validDays;
        });
    }

    // 情境：今日往後推一天，剩餘天數不會增加（單調不增）。
    [Fact(DisplayName = "今日往後推一天剩餘天數不增加（FR-011）")]
    public void Result_does_not_increase_as_days_pass()
    {
        Gen.Select(Gen.Int[-1000, 5000], Gen.Int[1, 3650]).Sample((daysAgo, validDays) =>
        {
            var last = Today.AddDays(-daysAgo);
            return PasswordExpiry.RemainingDays(last, Today.AddDays(1), validDays)
                <= PasswordExpiry.RemainingDays(last, Today, validDays);
        });
    }

    // 情境：當日修改密碼者剩餘天數等於效期天數（重新起算，FR-010）。
    // @SPEC-013 @AC-17
    [Fact(DisplayName = "當日修改密碼剩餘天數等於效期天數（FR-010）")]
    public void Same_day_equals_valid_days()
    {
        Gen.Int[1, 3650].Sample(validDays =>
            PasswordExpiry.RemainingDays(Today, Today, validDays) == validDays);
    }

    // 情境：剩餘天數為 0 時才視為已逾期。
    // @SPEC-013 @AC-19
    [Fact(DisplayName = "剩餘天數為 0 時視為已逾期（FR-011）")]
    public void Expired_iff_remaining_is_zero()
    {
        Gen.Select(Gen.Int[-1000, 5000], Gen.Int[1, 3650]).Sample((daysAgo, validDays) =>
        {
            var last = Today.AddDays(-daysAgo);
            return PasswordExpiry.IsExpired(last, Today, validDays)
                == (PasswordExpiry.RemainingDays(last, Today, validDays) == 0);
        });
    }
}
