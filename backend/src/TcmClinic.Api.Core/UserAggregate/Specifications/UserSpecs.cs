using Ardalis.Specification;

namespace TcmClinic.Api.Core.UserAggregate.Specifications;

/// <summary>依帳號取單一使用者（含所屬角色）。供登入、每請求重驗與唯一性檢查。</summary>
public sealed class UserByAccountSpec : Specification<User>
{
    public UserByAccountSpec(UserAccount account)
    {
        Query
            .Where(u => u.Account == account)
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .TagWith(nameof(UserByAccountSpec));
    }
}

/// <summary>
/// 依帳號取單一使用者並追蹤變更（含所屬角色）。供「修改本人個人資料」以登入帳號定位本人後更新（SPEC-013）。
/// </summary>
public sealed class UserByAccountForUpdateSpec : Specification<User>
{
    public UserByAccountForUpdateSpec(UserAccount account)
    {
        Query
            .Where(u => u.Account == account)
            .Include(u => u.UserRoles)
            .TagWith(nameof(UserByAccountForUpdateSpec));
    }
}

/// <summary>依對外識別碼（PublicId，SPEC-064）取單一使用者（含所屬角色，供修改時追蹤角色變更）。</summary>
public sealed class UserByPublicIdSpec : Specification<User>
{
    public UserByPublicIdSpec(UserPublicId publicId)
    {
        Query
            .Where(u => u.PublicId == publicId)
            .Include(u => u.UserRoles);
    }
}

/// <summary>依內部 ID 取單一使用者（含所屬角色，不追蹤）。供 refresh token 換新時確認帳號仍有效（SPEC-004 FR-021a）。</summary>
public sealed class UserByIdWithRolesSpec : Specification<User>
{
    public UserByIdWithRolesSpec(UserId id)
    {
        Query
            .Where(u => u.Id == id)
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .TagWith(nameof(UserByIdWithRolesSpec));
    }
}

/// <summary>全部使用者（未刪除，含所屬角色），依帳號排序。供清單查詢與防鎖死計數。</summary>
public sealed class UsersOrderedSpec : Specification<User>
{
    public UsersOrderedSpec()
    {
        Query
            .Include(u => u.UserRoles)
            .OrderBy(u => u.Account)
            .AsNoTracking()
            .TagWith(nameof(UsersOrderedSpec));
    }
}
