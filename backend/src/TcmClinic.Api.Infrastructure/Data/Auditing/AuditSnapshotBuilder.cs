using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data.Auditing;

/// <summary>一次異動的擷取結果（供攔截器組成 <see cref="AuditLog"/>）。</summary>
public sealed record AuditCapture(
    string EntityName,
    string TableName,
    string PrimaryKey,
    AuditOperation Operation,
    string? OldValues,
    string? NewValues);

/// <summary>
/// 由 EF <see cref="EntityEntry"/> 產生改前/改後整列快照與操作類型（FR-002/003/015），
/// 對標記 <see cref="SensitiveDataAttribute"/> 的欄位遮罩（FR-004），並執行 no-op 判定（FR-008）。
/// </summary>
public static class AuditSnapshotBuilder
{
    /// <summary>遮罩後的替代值。</summary>
    public const string Mask = "***";

    /// <summary>稽核/軟刪除元資料欄；no-op 判定時排除（避免 ModifyDate 每次刷新誤判為有變更）。</summary>
    private static readonly string[] MetadataColumns =
        ["CreateDate", "CreateUser", "ModifyDate", "ModifyUser"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 保留中文，不轉 \uXXXX
    };

    /// <summary>純判定：依狀態與軟刪除轉態決定操作類型。</summary>
    public static AuditOperation ClassifyState(EntityState state, bool removedTransitionedToTrue) => state switch
    {
        EntityState.Added => AuditOperation.Insert,
        EntityState.Deleted => AuditOperation.Delete,
        EntityState.Modified when removedTransitionedToTrue => AuditOperation.Delete,
        _ => AuditOperation.Update,
    };

    /// <summary>純判定：修改的欄位中是否存在「非元資料欄」的實質變更。</summary>
    public static bool HasBusinessChange(IEnumerable<string> modifiedPropertyNames) =>
        modifiedPropertyNames.Any(name => !MetadataColumns.Contains(name));

    /// <summary>純序列化：將「欄位名→值」列序列化為 JSON（中文不轉義）。</summary>
    public static string SerializeRow(IReadOnlyDictionary<string, object?> row) =>
        JsonSerializer.Serialize(row, JsonOptions);

    /// <summary>
    /// 由 entry 建立擷取結果；若為無實質變更之修改（no-op）回傳 <c>null</c>。
    /// 假設呼叫端已確認 <see cref="AuditScope.IsAudited"/>。
    /// </summary>
    public static AuditCapture? Build(EntityEntry entry)
    {
        var removedToTrue = RemovedTransitionedToTrue(entry);

        if (entry.State == EntityState.Modified && !removedToTrue)
        {
            var modified = entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name);
            if (!HasBusinessChange(modified))
            {
                return null; // no-op：僅元資料欄被刷新（FR-008）
            }
        }

        var operation = ClassifyState(entry.State, removedToTrue);
        var isDelete = operation == AuditOperation.Delete;
        var isInsert = operation == AuditOperation.Insert;

        var oldValues = isInsert ? null : SerializeRow(BuildRow(entry, useOriginal: true));
        var newValues = isDelete ? null : SerializeRow(BuildRow(entry, useOriginal: false));

        return new AuditCapture(
            entry.Metadata.ClrType.Name,
            entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
            BuildPrimaryKey(entry),
            operation,
            oldValues,
            newValues);
    }

    /// <summary>
    /// 存檔後補正擷取結果：主鍵與外鍵由資料庫 IDENTITY 配號（ADR-010），存檔前只是暫時值，
    /// 故以 entry 目前的值重算主鍵與改後快照；改前快照與操作類型沿用存檔前的擷取。刪除不需補正。
    /// </summary>
    public static AuditCapture RefreshAfterSave(AuditCapture captured, EntityEntry entry) =>
        captured.Operation == AuditOperation.Delete
            ? captured
            : captured with
            {
                PrimaryKey = BuildPrimaryKey(entry),
                NewValues = SerializeRow(BuildRow(entry, useOriginal: false)),
            };

    private static bool RemovedTransitionedToTrue(EntityEntry entry)
    {
        if (entry.State != EntityState.Modified)
        {
            return false;
        }

        var removed = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(ISoftDelete.Removed));
        return removed is not null
            && removed.OriginalValue is false
            && removed.CurrentValue is true;
    }

    private static Dictionary<string, object?> BuildRow(EntityEntry entry, bool useOriginal)
    {
        var row = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (IsSensitive(property))
            {
                row[name] = Mask;
                continue;
            }

            var value = useOriginal ? property.OriginalValue : property.CurrentValue;
            row[name] = ToProviderValue(property, value);
        }

        return row;
    }

    private static string BuildPrimaryKey(EntityEntry entry)
    {
        var keyProps = entry.Metadata.FindPrimaryKey()?.Properties ?? [];
        var parts = keyProps.Select(p =>
        {
            var pe = entry.Property(p.Name);
            var value = ToProviderValue(pe, pe.CurrentValue);
            return keyProps.Count == 1 ? $"{value}" : $"{p.Name}={value}";
        });
        return string.Join(";", parts);
    }

    // 將強型別/Vogen 值透過其 ValueConverter 取底層原生值；無轉換器則用原值。
    private static object? ToProviderValue(PropertyEntry property, object? value)
    {
        if (value is null)
        {
            return null;
        }

        var converter = property.Metadata.GetValueConverter();
        return converter is null ? value : converter.ConvertToProvider(value);
    }

    private static bool IsSensitive(PropertyEntry property) =>
        property.Metadata.PropertyInfo?.GetCustomAttribute<SensitiveDataAttribute>(inherit: true) is not null;
}
