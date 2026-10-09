namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 一次資料訪問事件的記錄請求（供敏感讀取/匯出/下載端點描述其訪問，FR-009）。
/// <paramref name="PrimaryKey"/> 單筆讀取填主鍵字串、匯出多筆時傳 <c>null</c>；
/// <paramref name="Purpose"/> 為選填的業務目的說明。「是否涉及敏感資料／敏感欄位清單」
/// 由記錄服務依實體型別的 <see cref="SensitiveDataAttribute"/> 標記自動判定，呼叫端無需指定（FR-010）。
/// </summary>
public sealed record AccessLogRequest(
    AccessType AccessType,
    string EntityName,
    string? PrimaryKey,
    int RecordCount,
    string? Purpose = null);

/// <summary>
/// 資料訪問記錄服務抽象（FR-015）。供敏感讀取/匯出/下載端點呼叫，以旁路、盡力寫入的方式
/// 將訪問事件記錄至 SysAccessLog（FR-014）：寫入失敗不得中斷或拖垮使用者主流程。
/// 記錄前依實體型別自動判定敏感欄位（FR-010）；一般讀取僅在涉及敏感欄位時記錄，匯出/下載一律記錄（FR-008）。
/// </summary>
public interface IAccessLogRecorder
{
    /// <summary>記錄對實體型別 <typeparamref name="TEntity"/> 的一次訪問。</summary>
    Task RecordAsync<TEntity>(AccessLogRequest request, CancellationToken cancellationToken = default);

    /// <summary>記錄對指定實體型別的一次訪問（非泛型版本）。</summary>
    Task RecordAsync(Type entityType, AccessLogRequest request, CancellationToken cancellationToken = default);
}
