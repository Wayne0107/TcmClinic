using TcmClinic.Api.Core.SessionAggregate;

namespace TcmClinic.Api.UseCases.Auth;

/// <summary>
/// session 輪替的條件式保存（SPEC-004 FR-021b）：只有資料庫中的目前 refresh token 雜湊仍為 <c>expectedTokenHash</c> 時才寫入，
/// 避免兩個請求同時以同一個 refresh token 換新時，後寫入者覆蓋先寫入者的輪替結果。實作於 Infrastructure。
/// </summary>
public interface ISessionRotationStore
{
    /// <summary>
    /// 保存已在記憶體中輪替（<see cref="UserSession.Rotate"/>）的 session。寫入成功回 true；已被其他請求輪替時不寫入、回 false，
    /// 並把 <paramref name="rotated"/> 重新載入為資料庫中的最新值。
    /// </summary>
    Task<bool> TryCommitRotationAsync(UserSession rotated, string expectedTokenHash, CancellationToken cancellationToken);
}
