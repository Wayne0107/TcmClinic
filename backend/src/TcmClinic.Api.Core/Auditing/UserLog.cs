namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 使用者操作紀錄（對應資料表 SysUserLog；SPEC-013，FR-015）。append-only 且不可變：一經建立即不更新、不軟刪除。
/// 記錄重要業務操作的成功／失敗，欄位對齊舊系統 USER_LOG，供「使用者存取記錄」查詢。
/// 依 DATABASE-SCHEMA §6 append-only 例外，僅主鍵採資料庫產生的 <see cref="long"/>；§1 五個標準欄位照常具備：
/// 「誰／何時」由攔截器填入 <see cref="CreateUser"/>／<see cref="CreateDate"/>，<see cref="ModifyUser"/>／<see cref="ModifyDate"/>
/// 與其同值，<see cref="Removed"/> 恆為 false（設計決策 R8，2026-09-30 修訂）。
/// 標記 <see cref="NotAuditedAttribute"/>：本表插入不得再產生 SysAuditLog。
/// 與 <see cref="AuditLog"/> 之別：後者只在資料成功寫入時產生；本表連「失敗」也記。內容不得含任何密碼。
/// </summary>
[NotAudited]
public sealed class UserLog : IAggregateRoot, IAuditable, ISoftDelete
{
    /// <summary>主鍵：操作紀錄流水號（資料庫產生）。</summary>
    public long Id { get; private set; }

    /// <summary>功能代碼（如 <c>PersonalData</c>）。</summary>
    public string FunctionId { get; private set; } = string.Empty;

    /// <summary>動作（如 <c>Modify</c>）。</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>結果（<see cref="UserLogStatus"/> 的名稱：Success／Failure）。</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>操作內容訊息（不得含密碼）。</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>來源用戶端 IP（背景作業或無法取得時為 <c>null</c>）。</summary>
    public string? ClientIp { get; private set; }

    /// <summary>操作時間（本地時區；攔截器於新增時填入）。</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>操作者帳號（無登入者時為 system／anonymous；攔截器於新增時填入）。</summary>
    public string CreateUser { get; set; } = string.Empty;

    /// <summary>與 <see cref="CreateDate"/> 同值（列不會被更新）。</summary>
    public DateTime ModifyDate { get; set; }

    /// <summary>與 <see cref="CreateUser"/> 同值（列不會被更新）。</summary>
    public string ModifyUser { get; set; } = string.Empty;

    /// <summary>恆為 false（列不會被軟刪除）。</summary>
    public bool Removed { get; set; }

    public UserLog(
        string functionId,
        string action,
        UserLogStatus status,
        string content,
        string? clientIp)
    {
        FunctionId = functionId;
        Action = action;
        Status = status.Name;
        Content = content;
        ClientIp = clientIp;
    }

    // EF Core 具現化用。
    private UserLog()
    {
    }
}
