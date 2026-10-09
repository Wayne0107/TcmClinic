using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Web.Services;

/// <summary>以 HttpContext 取得當前使用者名稱與來源 IP，供稽核攔截器與訪問日誌使用。</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <summary>
    /// 已驗證請求回傳登入帳號；請求存在但未驗證時為 <c>anonymous</c>；無 HttpContext（啟動種子、背景作業）時為
    /// <c>system</c>，與 Infrastructure 預設的 SystemCurrentUser 一致（SPEC-013 修正註冊順序後維持種子列的操作者標示）。
    /// </summary>
    public string UserName
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return "system";
            }

            return httpContext.User.Identity?.Name ?? "anonymous";
        }
    }

    /// <summary>來源用戶端 IP：優先取連線的 RemoteIpAddress；無 HttpContext（背景作業）時為 null（SPEC-010，FR-009）。</summary>
    public string? SourceIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
