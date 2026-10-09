using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data;

/// <summary>
/// 預設當前使用者（種子／設計時／背景作業用）。Web 層會以 HttpContext 版本覆寫。
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public string UserName => "system";
}
