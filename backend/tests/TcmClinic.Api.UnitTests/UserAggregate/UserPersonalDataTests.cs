using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.UserAggregate;

/// <summary>使用者個人資料欄位（SPEC-013）：聯絡資料與密碼修改時間。</summary>
public class UserPersonalDataTests
{
    private static User NewUser() =>
        new(UserAccount.From("bob"), UserName.From("Bob"));

    // 情境：新建使用者尚無聯絡資料與密碼修改時間。
    [Fact(DisplayName = "新建使用者的電話、電子郵件與密碼修改時間預設為空")]
    public void New_user_has_no_personal_data()
    {
        var user = NewUser();
        Assert.Null(user.Phone);
        Assert.Null(user.Email);
        Assert.Null(user.PasswordChangedAt);
    }

    // 情境：設定密碼時同時記錄修改時間（FR-010、FR-011）。
    [Fact(DisplayName = "設定密碼時同時記錄密碼修改時間（FR-010）")]
    public void SetPassword_records_changed_at()
    {
        var changedAt = new DateTime(2026, 8, 27, 10, 30, 0);
        var user = NewUser().SetPassword("hash", "salt", changedAt);

        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal("salt", user.PasswordSalt);
        Assert.Equal(changedAt, user.PasswordChangedAt);
    }

    // 情境：可設定聯絡資料，並以 null 清除（FR-003、Edge Case：送空值＝清除）。
    [Fact(DisplayName = "可設定聯絡電話與電子郵件並以 null 清除（FR-003）")]
    public void UpdateContact_sets_and_clears()
    {
        var user = NewUser().UpdateContact(PhoneNumber.From("02-1234"), EmailAddress.From("b@c.co"));
        Assert.Equal("02-1234", user.Phone!.Value.Value);
        Assert.Equal("b@c.co", user.Email!.Value.Value);

        user.UpdateContact(null, null);
        Assert.Null(user.Phone);
        Assert.Null(user.Email);
    }
}
