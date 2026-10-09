using Ardalis.SmartEnum;

namespace TcmClinic.Api.Core.RoleAggregate;

/// <summary>
/// 角色種類（SPEC-012）。以 SmartEnum 取代 plain enum（Principle IV），以 <see cref="SmartEnum{T}.Name"/>
/// 字串持久化，語意可讀。前端顯示：System↔「預設-(系統管理員)」、Custom↔「自訂-(一般使用者)」
/// （角色權限維護頁的角色種類選項）。
/// System 類角色不可刪除（FR-013）；SUPERADMIN 種為 Custom（維持 005 可刪除，不特別處理）。
/// </summary>
public sealed class RoleType : SmartEnum<RoleType>
{
    /// <summary>系統管理員類（前端「預設-(系統管理員)」）。不可刪除。</summary>
    public static readonly RoleType System = new(nameof(System), 1);

    /// <summary>一般使用者類（前端「自訂-(一般使用者)」）。可設定權限、未指派時可刪除。</summary>
    public static readonly RoleType Custom = new(nameof(Custom), 2);

    private RoleType(string name, int value) : base(name, value)
    {
    }
}
