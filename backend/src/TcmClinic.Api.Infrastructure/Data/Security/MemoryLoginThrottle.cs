using System.Collections.Concurrent;
using TcmClinic.Api.UseCases.Auth;

namespace TcmClinic.Api.Infrastructure.Data.Security;

/// <summary>連續 5 次登入失敗即鎖定 15 分鐘（FR-006a）；逾時自動解除。</summary>
/*
 * [維運提示] 登入失敗鎖定（FR-006a）
 * 依賴服務：無（行程內記憶體狀態）。
 * 監控關鍵：[LOGIN_LOCKED] 同一帳號連續 5 次失敗即鎖定 15 分鐘。
 * 注意：狀態為行程內——服務重啟即清空、多實例部署下各節點計數獨立。
 * 應變：若需跨實例一致的鎖定，改以分散式快取（如 Redis）替換本實作，介面不變。
 */
public sealed class MemoryLoginThrottle(TimeProvider timeProvider) : ILoginThrottle
{
    private const int MAX_FAILURES = 5;
    private static readonly TimeSpan LOCKOUT_DURATION = TimeSpan.FromMinutes(15);

    private sealed class Entry
    {
        public int FailureCount;
        public DateTimeOffset? LockedUntil;
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public bool IsLocked(string account)
    {
        if (!_entries.TryGetValue(account, out var entry) || entry.LockedUntil is null)
        {
            return false;
        }

        if (timeProvider.GetUtcNow() >= entry.LockedUntil.Value)
        {
            _entries.TryRemove(account, out _);
            return false;
        }

        return true;
    }

    public void RegisterFailure(string account)
    {
        var entry = _entries.GetOrAdd(account, static _ => new Entry());
        lock (entry)
        {
            entry.FailureCount++;
            if (entry.FailureCount >= MAX_FAILURES)
            {
                entry.LockedUntil = timeProvider.GetUtcNow().Add(LOCKOUT_DURATION);
            }
        }
    }

    public void Reset(string account) => _entries.TryRemove(account, out _);
}
