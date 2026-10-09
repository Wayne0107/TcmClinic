using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.UserAggregate;

public class UserTests
{
    private static User NewUser() =>
        new(UserAccount.From("bob"), UserName.From("Bob"));

    // 情境：設定密碼後 hash/salt 皆被賦值（FR-007）。
    // @SPEC-004 @AC-13
    [Fact(DisplayName = "設定密碼後 hash 與 salt 皆被賦值（FR-007）")]
    public void SetPassword_sets_hash_and_salt()
    {
        var user = NewUser().SetPassword("hash", "salt", DateTime.Now);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal("salt", user.PasswordSalt);
    }

    // 情境：登入時自動轉換雜湊只換 hash/salt，密碼修改時間不變——使用者沒有真的改密碼，
    // 密碼效期（SPEC-013 FR-010/011）不得因此重來（FR-007a）。
    // @SPEC-004 @AC-31
    [Fact(DisplayName = "轉換雜湊只換 hash 與 salt，不變更密碼修改時間（FR-007a）")]
    public void UpgradePasswordHash_keeps_password_changed_at()
    {
        var changedAt = new DateTime(2026, 1, 2, 3, 4, 5);
        var user = NewUser().SetPassword("old-hash", "old-salt", changedAt);

        user.UpgradePasswordHash("new-hash", "new-salt");

        Assert.Equal(("new-hash", "new-salt"), (user.PasswordHash, user.PasswordSalt));
        Assert.Equal(changedAt, user.PasswordChangedAt);
    }

    // 情境：重設角色為空集合 → 應擲例外（每位使用者至少一個角色，FR-016）。
    // @SPEC-004 @AC-16
    [Fact(DisplayName = "重設角色為空集合時擲例外（至少一個角色，FR-016）")]
    public void ReplaceRoles_rejects_empty()
    {
        var user = NewUser();
        Assert.Throws<InvalidOperationException>(() => user.ReplaceRoles([]));
    }

    // 情境：重設角色後，所屬角色數量與去重結果一致。
    [Fact(DisplayName = "重設角色後所屬角色數量與去重結果一致")]
    public void ReplaceRoles_sets_distinct_roles()
    {
        var user = NewUser().ReplaceRoles([RoleId.From(1), RoleId.From(2), RoleId.From(2)]);
        Assert.Equal(2, user.UserRoles.Count);
    }

    // 情境：改名後顯示名稱更新（FR-014）。
    [Fact(DisplayName = "改名後顯示名稱更新（FR-014）")]
    public void Rename_updates_display_name()
    {
        var user = NewUser().Rename(UserName.From("Bobby"));
        Assert.Equal("Bobby", user.DisplayName.Value);
    }
}
