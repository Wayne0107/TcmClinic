namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>
/// 密碼雜湊抽象（Principle I：Core 定義、Infrastructure 實作）。
/// 實作須以加鹽單向雜湊保存密碼，明文不落地（FR-007/008）。
/// </summary>
public interface IPasswordHasher
{
    /// <summary>以現行演算法將明文密碼雜湊，回傳雜湊字串與各自獨立的 Base64 salt。</summary>
    /// <param name="password">明文密碼。</param>
    /// <returns>(hash, salt)；hash 的格式由實作決定（可含演算法與參數前綴）。</returns>
    (string Hash, string Salt) Hash(string password);

    /// <summary>以儲存的 salt 重算雜湊並比對輸入密碼是否正確；舊演算法的雜湊同樣可驗證。</summary>
    /// <param name="password">待驗證的明文密碼。</param>
    /// <param name="hash">儲存的雜湊字串。</param>
    /// <param name="salt">儲存的 Base64 salt。</param>
    /// <returns>相符回傳 true，否則 false。</returns>
    bool Verify(string password, string hash, string salt);

    /// <summary>儲存的雜湊是否為舊演算法或較弱參數，應於登入成功時以現行參數重新雜湊（SPEC-004 FR-007a）。</summary>
    /// <param name="hash">儲存的雜湊字串。</param>
    bool NeedsRehash(string hash);
}
