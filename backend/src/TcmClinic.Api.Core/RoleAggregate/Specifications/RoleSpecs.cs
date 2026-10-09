using Ardalis.Specification;

namespace TcmClinic.Api.Core.RoleAggregate.Specifications;

/// <summary>全部角色（未刪除，由全域過濾器處理），依名稱排序。</summary>
public sealed class RolesOrderedSpec : Specification<Role>
{
    public RolesOrderedSpec()
    {
        Query.OrderBy(r => r.Name).AsNoTracking().TagWith(nameof(RolesOrderedSpec));
    }
}

/// <summary>
/// 全部角色含已軟刪除列（略過全域過濾器），僅供自動產碼取最大 R### 序號，避免回收已刪除角色代碼（012，設計決策 R5）。
/// </summary>
public sealed class AllRolesIncludingDeletedSpec : Specification<Role>
{
    public AllRolesIncludingDeletedSpec()
    {
        Query.IgnoreQueryFilters().AsNoTracking().TagWith(nameof(AllRolesIncludingDeletedSpec));
    }
}

/// <summary>依對外識別碼（PublicId，SPEC-064）取單一角色。</summary>
public sealed class RoleByPublicIdSpec : Specification<Role>
{
    public RoleByPublicIdSpec(RolePublicId publicId)
    {
        Query.Where(r => r.PublicId == publicId);
    }
}

/// <summary>依角色代碼取角色（供唯一性檢查）。</summary>
public sealed class RoleByCodeSpec : Specification<Role>
{
    public RoleByCodeSpec(RoleCode code)
    {
        Query.Where(r => r.Code == code);
    }
}

/// <summary>某角色的可見選單授權列。</summary>
public sealed class RoleMenuVisibilityByRoleSpec : Specification<RoleMenuVisibility>
{
    public RoleMenuVisibilityByRoleSpec(RoleId roleId)
    {
        Query.Where(v => v.RoleId == roleId);
    }
}

/// <summary>全部可見性授權列（未刪除，供執行期依角色聯集解析）。</summary>
public sealed class AllRoleMenuVisibilitiesSpec : Specification<RoleMenuVisibility>
{
    public AllRoleMenuVisibilitiesSpec()
    {
        Query.AsNoTracking().TagWith(nameof(AllRoleMenuVisibilitiesSpec));
    }
}
