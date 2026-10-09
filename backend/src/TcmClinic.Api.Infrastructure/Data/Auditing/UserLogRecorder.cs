using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data.Auditing;

/// <summary>
/// <see cref="IUserLogRecorder"/> 實作：以獨立 scope 的 <see cref="AppDbContext"/> 寫入 SysUserLog（SPEC-013，設計決策 R8）。
/// [USER_LOG] 監控關鍵字。獨立 DbContext 確保主交易失敗時失敗紀錄仍可落庫，且不會把請求範圍 DbContext
/// 中尚未儲存（或儲存失敗）的修改一併送出。寫入失敗「不」拋出，改留可觀測的錯誤告警。
/// 操作者與時間由 <c>AuditableEntityInterceptor</c> 填入標準欄位（CreateUser／CreateDate；設計決策 R8 2026-09-30 修訂），
/// 新 scope 的 <see cref="ICurrentUser"/> 仍在同一請求的非同步流程中，解析結果與請求相同。
/// </summary>
public sealed class UserLogRecorder(
    IServiceScopeFactory scopeFactory,
    ICurrentUser currentUser,
    ILogger<UserLogRecorder> logger) : IUserLogRecorder
{
    public async Task RecordAsync(UserLogRequest request, CancellationToken cancellationToken = default)
    {
        // 於建立新 scope 前取得 IP 與操作者（後者僅供失敗告警）；寫入的操作者由攔截器填入。
        var operatedBy = currentUser.UserName;
        var clientIp = currentUser.SourceIp;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.UserLogs.Add(new UserLog(
                request.FunctionId,
                request.Action,
                request.Status,
                request.Content,
                clientIp));

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // 盡力寫入：失敗不拋出（不拖垮主流程），但留可觀測告警。
            logger.LogError(
                ex,
                "[USER_LOG] 操作紀錄寫入失敗：FunctionId={FunctionId} Action={Action} OperatedBy={OperatedBy}",
                request.FunctionId, request.Action, operatedBy);
        }
    }
}
