namespace TcmClinic.Api.Core.UserAggregate;

/// <summary>
/// 密碼有效期限計算（SPEC-013，FR-011）。以上次修改密碼日為第 1 天：
/// 剩餘天數＝效期天數 −（今日 − 修改日）之天數，並限制於 [0, 效期天數]。
/// 對齊舊系統 <c>RemnantExpireDate</c>（效期 90 天：當日 90、31 天前 59）；舊系統逾期仍顯示 1，
/// 新系統改為 0 並明確標示逾期。上限用以吸收修改日晚於今日的時鐘異常。
/// </summary>
public static class PasswordExpiry
{
    /// <summary>計算密碼尚餘有效天數。</summary>
    /// <param name="lastChanged">上次修改密碼日。</param>
    /// <param name="today">今日。</param>
    /// <param name="validDays">密碼效期天數（≥ 1）。</param>
    /// <returns>介於 0 與 <paramref name="validDays"/> 之間的剩餘天數。</returns>
    public static int RemainingDays(DateOnly lastChanged, DateOnly today, int validDays)
    {
        var elapsed = today.DayNumber - lastChanged.DayNumber;
        return Math.Clamp(validDays - elapsed, 0, validDays);
    }

    /// <summary>密碼是否已逾期（剩餘天數為 0）。</summary>
    public static bool IsExpired(DateOnly lastChanged, DateOnly today, int validDays) =>
        RemainingDays(lastChanged, today, validDays) == 0;
}
