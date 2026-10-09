using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Infrastructure.Data.Auditing;

namespace TcmClinic.Api.UnitTests.Auditing;

/// <summary>
/// AuditSnapshotBuilder 純邏輯：操作分類（含軟刪除轉態）、no-op 判定（排除稽核欄）、
/// 快照序列化（中文不轉義、遮罩值原樣輸出）。EF 端擷取由整合測試覆蓋。
/// </summary>
public class AuditSnapshotBuilderTests
{
    // 情境：新增 → Insert。
    // @SPEC-006 @AC-2
    [Fact(DisplayName = "Added 狀態分類為 Insert")]
    public void Classifies_added_as_insert()
        => Assert.Equal(AuditOperation.Insert, AuditSnapshotBuilder.ClassifyState(EntityState.Added, false));

    // 情境：刪除 → Delete。
    // @SPEC-006 @AC-3
    [Fact(DisplayName = "Deleted 狀態分類為 Delete")]
    public void Classifies_deleted_as_delete()
        => Assert.Equal(AuditOperation.Delete, AuditSnapshotBuilder.ClassifyState(EntityState.Deleted, false));

    // 情境：修改且 Removed 轉為 true（軟刪除）→ Delete。
    // @SPEC-006 @AC-3
    [Fact(DisplayName = "Modified 且軟刪除轉態分類為 Delete")]
    public void Classifies_soft_delete_as_delete()
        => Assert.Equal(AuditOperation.Delete, AuditSnapshotBuilder.ClassifyState(EntityState.Modified, true));

    // 情境：一般修改 → Update。
    // @SPEC-006 @AC-1
    [Fact(DisplayName = "一般 Modified 分類為 Update")]
    public void Classifies_modified_as_update()
        => Assert.Equal(AuditOperation.Update, AuditSnapshotBuilder.ClassifyState(EntityState.Modified, false));

    // 情境：僅稽核元資料欄變更（ModifyDate/User）→ 無實質變更（no-op）。
    // @SPEC-006 @AC-10
    [Fact(DisplayName = "僅元資料欄變更視為無實質變更")]
    public void Only_metadata_change_is_noop()
        => Assert.False(AuditSnapshotBuilder.HasBusinessChange(["ModifyDate", "ModifyUser"]));

    // 情境：含業務欄變更 → 有實質變更。
    // @SPEC-006 @AC-10
    [Fact(DisplayName = "含業務欄變更視為有實質變更")]
    public void Business_change_is_detected()
        => Assert.True(AuditSnapshotBuilder.HasBusinessChange(["ModifyDate", "DisplayName"]));

    // 情境：序列化保留中文（不轉 \uXXXX）並含欄位值。
    [Fact(DisplayName = "序列化保留中文且含欄位值")]
    public void Serializes_row_with_readable_chinese()
    {
        var json = AuditSnapshotBuilder.SerializeRow(new Dictionary<string, object?>
        {
            ["DisplayName"] = "系統管理員",
        });

        Assert.Contains("系統管理員", json);
        Assert.DoesNotContain("\\u", json);
    }

    // 情境：遮罩值於序列化中原樣輸出（供攔截器對機密欄寫入 "***"）。
    // @SPEC-006 @AC-4
    [Fact(DisplayName = "遮罩值於序列化中原樣輸出")]
    public void Serializes_mask_value_as_is()
    {
        var json = AuditSnapshotBuilder.SerializeRow(new Dictionary<string, object?>
        {
            ["PasswordHash"] = AuditSnapshotBuilder.Mask,
        });

        Assert.Contains("***", json);
    }
}
