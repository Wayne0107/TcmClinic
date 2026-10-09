namespace TcmClinic.Api.UseCases.Auth;

/// <summary>
/// 登入失敗鎖定抽象（FR-006a）。以正規化帳號為鍵追蹤連續失敗與鎖定狀態。
/// 實作為行程內狀態（Infrastructure），不新增 SysUser 欄位。
/// </summary>
public interface ILoginThrottle
{
    /// <summary>該帳號是否處於鎖定期間。</summary>
    bool IsLocked(string account);

    /// <summary>登記一次登入失敗；達門檻即鎖定一段時間。</summary>
    void RegisterFailure(string account);

    /// <summary>清除該帳號的失敗計數與鎖定（登入成功時呼叫）。</summary>
    void Reset(string account);
}
