using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data.Auditing;

/// <summary>
/// 判定 EF entry 是否受資料異動紀錄稽核（FR-014 範圍與排除清單）。預設全覆蓋，排除：
/// 稽核表本身（避免遞迴）與標記 <see cref="NotAuditedAttribute"/> 之實體。
/// </summary>
public static class AuditScope
{
    private static readonly EntityState[] AuditedStates =
        [EntityState.Added, EntityState.Modified, EntityState.Deleted];

    /// <summary>純判定：型別是否在稽核範圍內（排除 AuditLog 與 [NotAudited]）。</summary>
    public static bool IsAuditedType(Type clrType)
    {
        if (clrType == typeof(AuditLog))
        {
            return false;
        }

        return !Attribute.IsDefined(clrType, typeof(NotAuditedAttribute), inherit: true);
    }

    /// <summary>entry 是否需產生異動紀錄（狀態為新增/修改/刪除，且型別在範圍內）。</summary>
    public static bool IsAudited(EntityEntry entry) =>
        AuditedStates.Contains(entry.State) && IsAuditedType(entry.Metadata.ClrType);
}
