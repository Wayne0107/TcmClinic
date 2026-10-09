using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Security;

/// <summary>登入失敗鎖定行為（FR-006a、SC-008）。以可控時鐘驗證逾時解除。</summary>
public class MemoryLoginThrottleTests
{
    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    // 情境：連續 5 次失敗後帳號被鎖定（FR-006a）。
    // @SPEC-004 @AC-20
    [Fact(DisplayName = "連續 5 次登入失敗後帳號被鎖定（FR-006a）")]
    public void Locks_after_five_failures()
    {
        var throttle = new MemoryLoginThrottle(new FakeTimeProvider());

        for (var i = 0; i < 4; i++)
        {
            throttle.RegisterFailure("acc");
        }
        Assert.False(throttle.IsLocked("acc"));

        throttle.RegisterFailure("acc");
        Assert.True(throttle.IsLocked("acc"));
    }

    // 情境：鎖定逾時（15 分鐘後）自動解除（SC-008）。
    // @SPEC-004 @AC-20
    [Fact(DisplayName = "鎖定逾時（15 分鐘後）自動解除（SC-008）")]
    public void Lockout_expires_after_window()
    {
        var clock = new FakeTimeProvider();
        var throttle = new MemoryLoginThrottle(clock);

        for (var i = 0; i < 5; i++)
        {
            throttle.RegisterFailure("acc");
        }
        Assert.True(throttle.IsLocked("acc"));

        clock.UtcNow = clock.UtcNow.AddMinutes(16);
        Assert.False(throttle.IsLocked("acc"));
    }

    // 情境：登入成功呼叫 Reset → 清除失敗計數與鎖定。
    // @SPEC-004 @AC-20
    [Fact(DisplayName = "登入成功呼叫 Reset 清除失敗計數與鎖定狀態")]
    public void Reset_clears_state()
    {
        var throttle = new MemoryLoginThrottle(new FakeTimeProvider());

        for (var i = 0; i < 5; i++)
        {
            throttle.RegisterFailure("acc");
        }
        throttle.Reset("acc");

        Assert.False(throttle.IsLocked("acc"));
    }

    // 情境：鎖定為行程內狀態——服務重啟（新實例）不帶前一行程之鎖定，構成唯一超管遭鎖定時的解救途徑（FR-022）。
    // @SPEC-004 @AC-22
    [Fact(DisplayName = "鎖定為行程內狀態，服務重啟不帶前一行程鎖定，作為超管解救途徑（FR-022）")]
    public void Lockout_does_not_persist_across_restart()
    {
        var clock = new FakeTimeProvider();
        var beforeRestart = new MemoryLoginThrottle(clock);

        for (var i = 0; i < 5; i++)
        {
            beforeRestart.RegisterFailure("admin");
        }
        Assert.True(beforeRestart.IsLocked("admin"));

        // 模擬服務重啟：行程內快取隨行程結束而清空，新實例無鎖定狀態。
        var afterRestart = new MemoryLoginThrottle(clock);
        Assert.False(afterRestart.IsLocked("admin"));
    }
}
