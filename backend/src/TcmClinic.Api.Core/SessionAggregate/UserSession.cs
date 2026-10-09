using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Core.SessionAggregate;

/// <summary>
/// 登入 session（資料表 SysUserSession；SPEC-004 FR-021～FR-021c，ADR-011）。一次登入一筆，承載目前 refresh token 的雜湊；
/// 每次換新即輪替並把到期時間延長為換新時＋8 小時。每 15 分鐘輪替一次，寫入資料異動紀錄只會產生大量雜湊變更，故標記
/// <see cref="NotAuditedAttribute"/>。
/// </summary>
[NotAudited]
public class UserSession : AuditableEntityBase<UserSessionId>, IAggregateRoot
{
    /// <summary>session 有效期（滑動）：建立或輪替後 8 小時（FR-021a）。</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    /// <summary>前一個 refresh token 視為多分頁並行換新的寬限（FR-021b）。</summary>
    public static readonly TimeSpan ReuseGrace = TimeSpan.FromSeconds(30);

    /// <summary>撤銷原因代碼（寫入 <see cref="RevokeReason"/>）。</summary>
    public static class RevokeReasons
    {
        /// <summary>使用者登出（FR-005）。</summary>
        public const string Logout = "Logout";

        /// <summary>寬限後重用已輪替的 refresh token，視為外洩（FR-021b）。</summary>
        public const string Reuse = "Reuse";

        /// <summary>帳號已刪除或無有效角色（FR-021a）。</summary>
        public const string UserInvalid = "UserInvalid";
    }

    /// <summary>對外識別碼（UUIDv7），即 access token 的 <c>sid</c>。</summary>
    public UserSessionPublicId PublicId { get; private set; }

    /// <summary>登入的使用者。</summary>
    public UserId UserId { get; private set; }

    /// <summary>目前 refresh token 的 SHA-256 雜湊。</summary>
    [SensitiveData]
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>前一個（已輪替掉的）refresh token 雜湊，供重用偵測（FR-021b）。</summary>
    [SensitiveData]
    public string? PreviousTokenHash { get; private set; }

    /// <summary>到期時間（本地時間）。</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>最後一次輪替時間；從未輪替為 null。</summary>
    public DateTime? LastRotatedAt { get; private set; }

    /// <summary>撤銷時間；null 表示未撤銷。</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>撤銷原因（<see cref="RevokeReasons"/>）。</summary>
    public string? RevokeReason { get; private set; }

    public UserSession(UserId userId, string tokenHash, DateTime now)
    {
        // 初始化為哨兵 0，存檔時由資料庫 IDENTITY 配號（ADR-010）。
        Id = UserSessionId.From(0);
        PublicId = UserSessionPublicId.New();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = now + Lifetime;
    }

    // EF Core 具現化用。
    private UserSession()
    {
    }

    /// <summary>未撤銷且未逾期。</summary>
    public bool IsActive(DateTime now) => RevokedAt is null && now < ExpiresAt;

    /// <summary>輪替 refresh token：舊值成為前一個，到期時間延長為 <paramref name="now"/>＋8 小時（FR-021a）。</summary>
    public void Rotate(string newTokenHash, DateTime now)
    {
        PreviousTokenHash = TokenHash;
        TokenHash = newTokenHash;
        LastRotatedAt = now;
        ExpiresAt = now + Lifetime;
    }

    /// <summary>距最後一次輪替是否仍在寬限內（FR-021b）；從未輪替則否。</summary>
    public bool IsWithinReuseGrace(DateTime now) => LastRotatedAt is { } rotatedAt && now - rotatedAt <= ReuseGrace;

    /// <summary>撤銷 session；已撤銷者保留第一次的時間與原因。</summary>
    public void Revoke(string reason, DateTime now)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokeReason = reason;
    }
}
