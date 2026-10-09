using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>
/// 使用者聚合根（對應資料表 SysUser）。帳號為登入識別碼（未刪除範圍內唯一，FR-012）；
/// 密碼以加鹽 PBKDF2 保存，明文不落地（FR-007/009、ADR-009）；與角色為多對多，且至少一個角色（FR-016）。
/// </summary>
public class User : AuditableEntityBase<UserId>, IAggregateRoot
{
    /// <summary>對外識別碼（UUIDv7，建構時產生、不可變更；API 一律使用此值，SPEC-064）。</summary>
    public UserPublicId PublicId { get; private set; }

    private readonly List<UserRole> _userRoles = [];

    public UserAccount Account { get; private set; }

    public UserName DisplayName { get; private set; }

    [SensitiveData] // 憑證：不得寫入異動紀錄（FR-004、Principle XVII）。
    public string PasswordHash { get; private set; } = string.Empty;

    [SensitiveData] // 憑證：不得寫入異動紀錄（FR-004、Principle XVII）。
    public string PasswordSalt { get; private set; } = string.Empty;

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    /// <summary>聯絡電話（選填；SPEC-013，FR-005）。聯絡資訊非憑證，不標記 SensitiveData（設計決策 R6）。</summary>
    public PhoneNumber? Phone { get; private set; }

    /// <summary>電子郵件（選填；SPEC-013，FR-006）。</summary>
    public EmailAddress? Email { get; private set; }

    /// <summary>
    /// 上次修改密碼時間（本地時間；SPEC-013，FR-011）。null 表示本功能上線前建立的帳號，
    /// 計算效期時以 <c>CreateDate</c> 代替（設計決策 R5）。
    /// </summary>
    public DateTime? PasswordChangedAt { get; private set; }

    public User(UserAccount account, UserName displayName)
    {
        // 初始化為哨兵 0，存檔時由資料庫 IDENTITY 配號（ADR-010）。
        Id = UserId.From(0);
        PublicId = UserPublicId.New();
        Account = account;
        DisplayName = displayName;
    }

    // EF Core 具現化用。
    private User()
    {
    }

    /// <summary>
    /// 設定密碼雜湊與 salt（明文永不進入網域，FR-007），並記錄修改時間供效期計算（SPEC-013，FR-010/011）。
    /// 所有設定密碼的路徑（新增、重設、本人變更、種子）皆經此方法。
    /// </summary>
    public User SetPassword(string hash, string salt, DateTime changedAt)
    {
        PasswordHash = hash;
        PasswordSalt = salt;
        PasswordChangedAt = changedAt;
        return this;
    }

    /// <summary>
    /// 登入時將舊雜湊轉為現行演算法（SPEC-004 FR-007a）：只換雜湊與 salt，**不變更** <see cref="PasswordChangedAt"/>——
    /// 使用者沒有真的改密碼，密碼效期（SPEC-013 FR-010/011）不得因此重來。改密碼一律用 <see cref="SetPassword"/>。
    /// </summary>
    public User UpgradePasswordHash(string hash, string salt)
    {
        PasswordHash = hash;
        PasswordSalt = salt;
        return this;
    }

    /// <summary>設定聯絡電話與電子郵件；null 表示清除（SPEC-013，FR-003）。</summary>
    public User UpdateContact(PhoneNumber? phone, EmailAddress? email)
    {
        Phone = phone;
        Email = email;
        return this;
    }

    /// <summary>修改顯示名稱（FR-014）。帳號不提供修改方法（避免識別碼漂移）。</summary>
    public User Rename(UserName displayName)
    {
        DisplayName = displayName;
        return this;
    }

    /// <summary>
    /// 重設所屬角色；至少須有一個（FR-016）。既有關聯清除後由攔截器轉為軟刪除，
    /// 過濾唯一索引允許同 (UserId,RoleId) 於再次指派時重用（Removed=0）。
    /// </summary>
    public User ReplaceRoles(IEnumerable<RoleId> roleIds)
    {
        var ids = roleIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
        {
            throw new InvalidOperationException("每位使用者至少需保留一個角色。");
        }

        _userRoles.Clear();
        foreach (var roleId in ids)
        {
            _userRoles.Add(new UserRole(roleId));
        }

        return this;
    }
}
