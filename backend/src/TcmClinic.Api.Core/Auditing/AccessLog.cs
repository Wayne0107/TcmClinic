namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 資料訪問紀錄（對應資料表 SysAccessLog）。append-only 且不可變（FR-004）：一經建立即不更新、不軟刪除。
/// 依 DATABASE-SCHEMA §6 append-only 例外，僅主鍵採資料庫產生的 <see cref="long"/>（bigint IDENTITY，非 Vogen，
/// 故不繼承 <c>AuditableEntityBase</c>）；§1 五個標準欄位照常具備：「誰／何時」由攔截器填入
/// <see cref="CreateUser"/>／<see cref="CreateDate"/>，<see cref="ModifyUser"/>／<see cref="ModifyDate"/> 與其同值，
/// <see cref="Removed"/> 恆為 false（設計決策 R7，2026-09-30 修訂）。
/// 標記 <see cref="NotAuditedAttribute"/>：本表插入不得再產生 SysAuditLog（避免旁路記錄互相污染）。
/// 與 006 <c>SysAuditLog</c> 之別：後者記「資料異動（寫入）」整列快照；本表記「資料訪問（讀取/匯出/下載）」，
/// 僅記被訪問的敏感欄位「名稱」清單，機密「值」永不入庫（FR-005）。
/// </summary>
[NotAudited]
public sealed class AccessLog : IAggregateRoot, IAuditable, ISoftDelete
{
    /// <summary>主鍵：訪問紀錄流水號（資料庫產生）。</summary>
    public long Id { get; private set; }

    /// <summary>訪問類型（<see cref="AccessType"/> 的名稱：Read／Export／Download）。</summary>
    public string AccessType { get; private set; } = string.Empty;

    /// <summary>被訪問的實體型別名（如 <c>User</c>）。</summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>被訪問資料的主鍵字串（單筆讀取如 <c>123</c>；匯出多筆時為 <c>null</c>）。</summary>
    public string? PrimaryKey { get; private set; }

    /// <summary>本次訪問涉及的資料筆數（單筆讀取通常為 1，匯出/下載為實際筆數）。</summary>
    public int RecordCount { get; private set; }

    /// <summary>來源用戶端 IP（IPv4/IPv6；背景作業或無法取得時為 <c>null</c>）。</summary>
    public string? ClientIp { get; private set; }

    /// <summary>是否涉及敏感資料（被訪問實體含 <see cref="SensitiveDataAttribute"/> 標記欄位）。</summary>
    public bool InvolvesSensitiveData { get; private set; }

    /// <summary>被訪問的敏感欄位「名稱」清單 JSON（如 <c>["PasswordHash","PasswordSalt"]</c>；僅記名不記值，FR-005）。</summary>
    public string? SensitiveFieldsAccessed { get; private set; }

    /// <summary>訪問目的或原因說明（選填，供稽核追溯）。</summary>
    public string? Purpose { get; private set; }

    /// <summary>訪問發生時間（本地時區；攔截器於新增時填入）。</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>訪問者識別（帳號或系統標識，FR-007；攔截器於新增時填入）。</summary>
    public string CreateUser { get; set; } = string.Empty;

    /// <summary>與 <see cref="CreateDate"/> 同值（列不會被更新）。</summary>
    public DateTime ModifyDate { get; set; }

    /// <summary>與 <see cref="CreateUser"/> 同值（列不會被更新）。</summary>
    public string ModifyUser { get; set; } = string.Empty;

    /// <summary>恆為 false（列不會被軟刪除）。</summary>
    public bool Removed { get; set; }

    public AccessLog(
        AccessType accessType,
        string entityName,
        string? primaryKey,
        int recordCount,
        string? clientIp,
        bool involvesSensitiveData,
        string? sensitiveFieldsAccessed,
        string? purpose)
    {
        AccessType = accessType.Name;
        EntityName = entityName;
        PrimaryKey = primaryKey;
        RecordCount = recordCount;
        ClientIp = clientIp;
        InvolvesSensitiveData = involvesSensitiveData;
        SensitiveFieldsAccessed = sensitiveFieldsAccessed;
        Purpose = purpose;
    }

    // EF Core 具現化用。
    private AccessLog()
    {
    }
}
