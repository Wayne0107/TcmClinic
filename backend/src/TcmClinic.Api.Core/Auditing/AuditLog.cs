namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 資料異動紀錄（對應資料表 SysAuditLog）。append-only 且不可變（FR-010）：一經建立即不更新、不軟刪除。
/// 依 DATABASE-SCHEMA §6 append-only 例外，僅主鍵採資料庫產生的 <see cref="long"/>（bigint IDENTITY，非 Vogen，
/// 故不繼承 <c>AuditableEntityBase</c>）；§1 五個標準欄位照常具備：<see cref="CreateUser"/>／<see cref="CreateDate"/>
/// 記「誰／何時」，<see cref="ModifyUser"/>／<see cref="ModifyDate"/> 與其同值，<see cref="Removed"/> 恆為 false
/// （設計決策 R12，2026-09-30 修訂）。
/// 四個審計欄位由建構子一次填入：產生本紀錄的 <c>AuditLogInterceptor</c> 在 <c>AuditableEntityInterceptor</c> 之後執行，
/// 新增的本實體不會再被後者填值。
/// </summary>
public sealed class AuditLog : IAggregateRoot, IAuditable, ISoftDelete
{
    /// <summary>主鍵：異動紀錄流水號（資料庫產生）。</summary>
    public long Id { get; private set; }

    /// <summary>受異動的實體型別名（如 <c>User</c>）。</summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>受異動的資料表名（如 <c>SysUser</c>）。</summary>
    public string TableName { get; private set; } = string.Empty;

    /// <summary>目標資料列主鍵字串（複合鍵以 <c>k=v;k=v</c> 表示）。</summary>
    public string PrimaryKey { get; private set; } = string.Empty;

    /// <summary>操作類型（<see cref="AuditOperation"/> 的名稱：Insert／Update／Delete）。</summary>
    public string Operation { get; private set; } = string.Empty;

    /// <summary>改前整列快照 JSON（Insert 為 <c>null</c>；憑證／機密已遮罩）。</summary>
    public string? OldValues { get; private set; }

    /// <summary>改後整列快照 JSON（Delete 為 <c>null</c>；憑證／機密已遮罩）。</summary>
    public string? NewValues { get; private set; }

    /// <summary>異動發生時間（本地時區）。</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>操作者識別（帳號或系統標識，FR-009）。</summary>
    public string CreateUser { get; set; } = string.Empty;

    /// <summary>與 <see cref="CreateDate"/> 同值（列不會被更新）。</summary>
    public DateTime ModifyDate { get; set; }

    /// <summary>與 <see cref="CreateUser"/> 同值（列不會被更新）。</summary>
    public string ModifyUser { get; set; } = string.Empty;

    /// <summary>恆為 false（列不會被軟刪除）。</summary>
    public bool Removed { get; set; }

    public AuditLog(
        string entityName,
        string tableName,
        string primaryKey,
        AuditOperation operation,
        string? oldValues,
        string? newValues,
        string operatedBy,
        DateTime operatedAt)
    {
        EntityName = entityName;
        TableName = tableName;
        PrimaryKey = primaryKey;
        Operation = operation.Name;
        OldValues = oldValues;
        NewValues = newValues;
        CreateUser = operatedBy;
        CreateDate = operatedAt;
        ModifyUser = operatedBy;
        ModifyDate = operatedAt;
    }

    // EF Core 具現化用。
    private AuditLog()
    {
    }
}
