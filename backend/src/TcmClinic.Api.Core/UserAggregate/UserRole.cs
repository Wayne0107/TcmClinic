using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>
/// 使用者↔角色關聯（對應資料表 SysUserRole）。<see cref="UserId"/> 由 EF 依導覽關係填入。
/// </summary>
public class UserRole : AuditableEntityBase<UserRoleId>
{
    public UserId UserId { get; private set; }

    public RoleId RoleId { get; private set; }

    public UserRole(RoleId roleId)
    {
        // 初始化為哨兵 0，存檔時由資料庫 IDENTITY 配號（ADR-010）。
        Id = UserRoleId.From(0);
        // UserId 亦初始化為哨兵，避免 EF 對未初始化 Vogen 結構呼叫 GetHashCode 而拋錯；
        // 由 User→UserRoles 導覽關係於存檔時填入實際外鍵。
        UserId = UserId.From(0);
        RoleId = roleId;
    }

    // EF Core 具現化用。
    private UserRole()
    {
    }
}
