using CsCheck;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.UnitTests.SessionAggregate;

/// <summary>
/// 登入 session（SPEC-004 FR-021a／FR-021b，ADR-011）：建立時有效 8 小時；輪替時舊值記為前一個、到期時間滑動；
/// 撤銷或逾期即失效；前一個 refresh token 只在輪替後 30 秒內視為並行換新。
/// </summary>
public class UserSessionTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 9, 0, 0);

    private static UserSession NewSession() => new(UserId.From(1), "hash-1", Now);

    // 情境：新建 session 有 UUIDv7 對外識別碼、到期時間為現在＋8 小時，且為有效狀態。
    // @SPEC-004 @AC-33
    [Fact(DisplayName = "新建 session 有效 8 小時（SPEC-004 FR-021a）")]
    public void New_session_is_active_for_eight_hours()
    {
        var session = NewSession();

        Assert.Equal(7, session.PublicId.Value.Version);
        Assert.Equal(Now.AddHours(8), session.ExpiresAt);
        Assert.True(session.IsActive(Now));
        Assert.False(session.IsActive(Now.AddHours(8)));
    }

    // 情境：輪替後目前值換成新雜湊、舊值成為前一個，到期時間改為輪替時＋8 小時（滑動）。
    // @SPEC-004 @AC-33
    [Fact(DisplayName = "輪替後舊值成為前一個且到期時間滑動（SPEC-004 FR-021a）")]
    public void Rotate_keeps_previous_and_slides_expiry()
    {
        var session = NewSession();
        var later = Now.AddHours(7);

        session.Rotate("hash-2", later);

        Assert.Equal("hash-2", session.TokenHash);
        Assert.Equal("hash-1", session.PreviousTokenHash);
        Assert.Equal(later, session.LastRotatedAt);
        Assert.Equal(later.AddHours(8), session.ExpiresAt);
        Assert.True(session.IsActive(Now.AddHours(10)));
    }

    // 情境：撤銷後即失效；重複撤銷保留第一次的時間與原因。
    // @SPEC-004 @AC-34
    [Fact(DisplayName = "撤銷後失效且保留第一次撤銷原因（SPEC-004 FR-005、FR-021b）")]
    public void Revoke_deactivates_and_keeps_first_reason()
    {
        var session = NewSession();

        session.Revoke(UserSession.RevokeReasons.Logout, Now.AddMinutes(1));
        session.Revoke(UserSession.RevokeReasons.Reuse, Now.AddMinutes(2));

        Assert.False(session.IsActive(Now.AddMinutes(1)));
        Assert.Equal(Now.AddMinutes(1), session.RevokedAt);
        Assert.Equal(UserSession.RevokeReasons.Logout, session.RevokeReason);
    }

    // 情境：前一個 refresh token 在輪替後 30 秒內（含）屬寬限，超過即否；從未輪替則無寬限。
    // @SPEC-004 @AC-34
    // @SPEC-004 @AC-35
    [Fact(DisplayName = "前一個 refresh token 只在輪替後 30 秒內屬寬限（SPEC-004 FR-021b）")]
    public void Reuse_grace_is_thirty_seconds_after_rotation()
    {
        var session = NewSession();
        Assert.False(session.IsWithinReuseGrace(Now));

        session.Rotate("hash-2", Now);

        Assert.True(session.IsWithinReuseGrace(Now.AddSeconds(30)));
        Assert.False(session.IsWithinReuseGrace(Now.AddSeconds(31)));
    }

    // 情境（不變式）：產生的 refresh token 為 43 字元 Base64Url（32 bytes），雜湊為 64 字元小寫十六進位且與再次計算一致；
    // 每次產生的值都不同。
    // @SPEC-004 @AC-24
    [Fact(DisplayName = "refresh token 為 32 bytes 隨機值且只保存 SHA-256 雜湊（SPEC-004 FR-021）")]
    public void Refresh_token_is_random_and_hashed()
    {
        Gen.Const(0).Sample(_ =>
        {
            var (token, hash) = RefreshTokenValue.Generate();
            return token.Length == 43
                && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
                && hash.Length == 64
                && hash == RefreshTokenValue.Hash(token)
                && hash.All(c => char.IsAsciiDigit(c) || c is >= 'a' and <= 'f');
        });

        Assert.NotEqual(RefreshTokenValue.Generate().Token, RefreshTokenValue.Generate().Token);
    }
}
