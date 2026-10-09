using System.Reflection;

namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 依實體型別的宣告式 <see cref="SensitiveDataAttribute"/> 標記，判定「是否涉及敏感資料」並列出
/// 被標記的敏感欄位「名稱」（FR-010）。沿用 006 的宣告式標記機制：新增敏感欄位時無需改動記錄流程。
/// 純反射、無副作用，機密「值」永不經手（FR-005）——僅回傳欄位名，供訪問日誌記錄。
/// </summary>
public static class SensitiveFieldDetector
{
    /// <summary>回傳 <paramref name="entityType"/> 上所有標記 <see cref="SensitiveDataAttribute"/> 的屬性名（依宣告序）。</summary>
    public static IReadOnlyList<string> GetSensitiveFieldNames(Type entityType) =>
        entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SensitiveDataAttribute>(inherit: true) is not null)
            .Select(p => p.Name)
            .ToList();

    /// <summary>該實體型別是否含任一敏感欄位（列層級判定：型別上有任何 <see cref="SensitiveDataAttribute"/>）。</summary>
    public static bool InvolvesSensitiveData(Type entityType) =>
        GetSensitiveFieldNames(entityType).Count > 0;
}
