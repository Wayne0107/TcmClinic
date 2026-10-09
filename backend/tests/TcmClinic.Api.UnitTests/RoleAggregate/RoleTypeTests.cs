using TcmClinic.Api.Core.RoleAggregate;

namespace TcmClinic.Api.UnitTests.RoleAggregate;

/// <summary>角色種類與角色種類相關領域行為（SPEC-012，FR-004/013）。</summary>
public class RoleTypeTests
{
    private static Role NewRole(RoleType? type = null) =>
        new(RoleCode.From("R001"), RoleName.From("測試角色"), null, type);

    [Fact(DisplayName = "RoleType 僅有 System 與 Custom 兩值（012）")]
    public void RoleType_has_system_and_custom()
    {
        Assert.Equal("System", RoleType.System.Name);
        Assert.Equal("Custom", RoleType.Custom.Name);
        Assert.Equal(2, RoleType.List.Count);
    }

    [Fact(DisplayName = "未指定時角色種類預設為 Custom（012）")]
    public void Role_defaults_to_custom()
    {
        var role = NewRole();
        Assert.Equal(RoleType.Custom, role.RoleType);
        Assert.False(role.IsSystemRole);
    }

    // @SPEC-012 @AC-14
    [Fact(DisplayName = "指定 System 時 IsSystemRole 為真（012，FR-013）")]
    public void Role_system_is_system_role()
    {
        var role = NewRole(RoleType.System);
        Assert.Equal(RoleType.System, role.RoleType);
        Assert.True(role.IsSystemRole);
    }

    [Fact(DisplayName = "UpdateRoleType 可變更角色種類（012）")]
    public void UpdateRoleType_changes_type()
    {
        var role = NewRole();
        role.UpdateRoleType(RoleType.System);
        Assert.True(role.IsSystemRole);

        role.UpdateRoleType(RoleType.Custom);
        Assert.False(role.IsSystemRole);
    }

    [Fact(DisplayName = "RoleType.FromName 可由字串還原（供 EF 轉換／API 解析）")]
    public void FromName_roundtrips()
    {
        Assert.Equal(RoleType.System, RoleType.FromName("System"));
        Assert.Equal(RoleType.Custom, RoleType.FromName("Custom"));
        Assert.False(RoleType.TryFromName("Nope", out _));
    }
}
