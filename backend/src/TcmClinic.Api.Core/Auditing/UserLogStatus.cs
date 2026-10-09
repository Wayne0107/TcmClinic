namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 使用者操作結果（SPEC-013，FR-015；Principle II 以 SmartEnum 取代 plain enum）。
/// 以 <see cref="SmartEnum{T}.Name"/> 字串持久化，對齊舊系統 USER_LOG.STATUS（Success／Failure）。
/// </summary>
public sealed class UserLogStatus : SmartEnum<UserLogStatus>
{
    public static readonly UserLogStatus Success = new(nameof(Success), 1);
    public static readonly UserLogStatus Failure = new(nameof(Failure), 2);

    private UserLogStatus(string name, int value) : base(name, value)
    {
    }
}
