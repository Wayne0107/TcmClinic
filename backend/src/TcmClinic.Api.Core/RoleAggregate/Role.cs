namespace TcmClinic.Api.Core.RoleAggregate;

/// <summary>
/// 角色聚合根（對應資料表 SysRole）。005 起無超級管理員特例：角色一律平等，可編輯／刪除，
/// 授權由「角色↔選單可見性」決定。
/// </summary>
public class Role : AuditableEntityBase<RoleId>, IAggregateRoot
{
    /// <summary>對外識別碼（UUIDv7，建構時產生、不可變更；API 一律使用此值，SPEC-064）。</summary>
    public RolePublicId PublicId { get; private set; }

    /// <summary>內建管理角色代碼（僅供種子命名；不再具授權特權）。</summary>
    public const string SuperAdminCode = "SUPERADMIN";

    public RoleCode Code { get; private set; }
    public RoleName Name { get; private set; }
    public string? Description { get; private set; }

    /// <summary>角色種類（012）：System 系統管理員類／Custom 一般使用者類。預設 Custom。</summary>
    public RoleType RoleType { get; private set; } = RoleType.Custom;

    /// <summary>樂觀併發權杖（012）：防止兩位管理員同時「設定權限」時一方變更被靜默覆蓋（FR-015）。</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>是否為系統管理員類（不可刪除，FR-013）。</summary>
    public bool IsSystemRole => RoleType == RoleType.System;

    public Role(RoleCode code, RoleName name, string? description = null, RoleType? roleType = null)
    {
        // 初始化為哨兵 0（已初始化的 VO），避免 EF 追蹤時對未初始化 Vogen 結構呼叫
        // GetHashCode 而拋錯；存檔時由資料庫 IDENTITY 配號（ADR-010）。
        Id = RoleId.From(0);
        PublicId = RolePublicId.New();
        Code = code;
        Name = name;
        Description = description;
        RoleType = roleType ?? RoleType.Custom;
    }

    // EF Core 具現化用。
    private Role()
    {
    }

    public Role UpdateName(RoleName name)
    {
        Name = name;
        return this;
    }

    public Role UpdateDescription(string? description)
    {
        Description = description;
        return this;
    }

    /// <summary>變更角色種類（012）。</summary>
    public Role UpdateRoleType(RoleType roleType)
    {
        RoleType = roleType;
        return this;
    }
}
