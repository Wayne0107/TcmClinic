using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data.Auditing;

/// <summary>
/// <see cref="IAccessLogRecorder"/> 實作：以旁路、盡力寫入方式將資料訪問事件寫入 <c>SysAccessLog</c>（SPEC-010）。
/// [ACCESS_LOG] 監控關鍵字（Principle X）。依實體型別的 <see cref="SensitiveDataAttribute"/> 自動判定敏感欄位（FR-010）；
/// 一般讀取僅在涉及敏感欄位時記錄、匯出/下載一律記錄（FR-008）；僅記敏感欄位「名稱」，機密「值」永不入庫（FR-005）。
/// 寫入失敗「不」拋出（FR-014）：不得中斷或拖垮使用者查詢/匯出主流程，改留可觀測的錯誤告警。
/// 訪問者與時間由 <c>AuditableEntityInterceptor</c> 填入標準欄位（CreateUser／CreateDate；設計決策 R7 2026-09-30 修訂）。
/// </summary>
public sealed class AccessLogRecorder(
    AppDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<AccessLogRecorder> logger) : IAccessLogRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 敏感欄位名多為 ASCII，保留原樣不轉義
    };

    public Task RecordAsync<TEntity>(AccessLogRequest request, CancellationToken cancellationToken = default) =>
        RecordAsync(typeof(TEntity), request, cancellationToken);

    public async Task RecordAsync(
        Type entityType, AccessLogRequest request, CancellationToken cancellationToken = default)
    {
        AccessLog? log = null;
        try
        {
            var sensitiveFields = SensitiveFieldDetector.GetSensitiveFieldNames(entityType);
            var involvesSensitive = sensitiveFields.Count > 0;

            // FR-008：一般讀取（Read）僅在涉及敏感欄位時記錄，避免日誌爆量；匯出/下載一律記錄。
            if (request.AccessType == AccessType.Read && !involvesSensitive)
            {
                return;
            }

            // FR-005：僅序列化敏感欄位「名稱」清單，機密值永不經手。
            var sensitiveFieldsJson = involvesSensitive
                ? JsonSerializer.Serialize(sensitiveFields, JsonOptions)
                : null;

            log = new AccessLog(
                request.AccessType,
                request.EntityName,
                request.PrimaryKey,
                request.RecordCount,
                currentUser.SourceIp,
                involvesSensitive,
                sensitiveFieldsJson,
                request.Purpose);

            dbContext.Set<AccessLog>().Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // FR-014：盡力寫入，失敗不拋出（不拖垮主流程），但留可觀測告警。
            logger.LogError(
                ex,
                "[ACCESS_LOG] 訪問日誌寫入失敗：EntityName={EntityName} AccessType={AccessType} AccessedBy={AccessedBy}",
                request.EntityName, request.AccessType.Name, currentUser.UserName);

            // 卸除未落庫的紀錄，避免污染共享 DbContext 的 ChangeTracker。
            if (log is not null)
            {
                dbContext.Entry(log).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
        }
    }
}
