using TcmClinic.Api.Core.MenuAggregate;

namespace TcmClinic.Api.Core.RoleAggregate;

/// <summary>
/// 角色可見選單（對應資料表 SysRoleTask）。僅授權「具路由的選單子項」；
/// 容器不入表，執行期自動回溯顯示（FR-011/012）。
/// </summary>
public class RoleMenuVisibility : AuditableEntityBase<RoleMenuVisibilityId>, IAggregateRoot
{
    public RoleId RoleId { get; private set; }
    public MenuItemId MenuItemId { get; private set; }

    public RoleMenuVisibility(RoleId roleId, MenuItemId menuItemId)
    {
        Id = RoleMenuVisibilityId.From(0);
        RoleId = roleId;
        MenuItemId = menuItemId;
    }

    // EF Core 具現化用。
    private RoleMenuVisibility()
    {
    }
}
