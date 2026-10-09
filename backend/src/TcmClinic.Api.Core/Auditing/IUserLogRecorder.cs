namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 一次使用者操作的記錄請求（SPEC-013，FR-015）。操作者與來源 IP 由記錄服務自目前使用者取得，
/// 呼叫端無需指定。<paramref name="Content"/> 不得含任何密碼。
/// </summary>
public sealed record UserLogRequest(
    string FunctionId,
    string Action,
    UserLogStatus Status,
    string Content);

/// <summary>
/// 使用者操作記錄服務抽象（SPEC-013，設計決策 R8）。以獨立交易、盡力寫入的方式寫入 SysUserLog：
/// 主交易失敗時失敗紀錄仍須落庫且不得帶出主交易未儲存的修改；寫入失敗不得中斷使用者主流程。
/// </summary>
public interface IUserLogRecorder
{
    /// <summary>記錄一次使用者操作。</summary>
    Task RecordAsync(UserLogRequest request, CancellationToken cancellationToken = default);
}
