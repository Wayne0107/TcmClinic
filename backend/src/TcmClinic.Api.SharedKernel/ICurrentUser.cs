namespace TcmClinic.Api.SharedKernel;

/// <summary>當前操作者抽象；供稽核攔截器填入 Create/Modify User，並供訪問日誌取得來源 IP。</summary>
public interface ICurrentUser
{
    string UserName { get; }

    /// <summary>
    /// 當前請求的來源用戶端 IP（供資料訪問日誌記錄，SPEC-010）。
    /// 背景作業／種子／無 HttpContext 時為 <c>null</c>（允許空值，不 fail hard）。
    /// 預設回傳 <c>null</c>；Web 層以 HttpContext 版本覆寫提供實際 IP。
    /// </summary>
    string? SourceIp => null;
}
