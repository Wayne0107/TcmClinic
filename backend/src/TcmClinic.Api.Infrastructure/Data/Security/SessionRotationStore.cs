using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Auth;

namespace TcmClinic.Api.Infrastructure.Data.Security;

/// <summary>
/// session 輪替的條件式保存（SPEC-004 FR-021b）：以 <c>UPDATE … WHERE Id = @id AND TokenHash = @expected</c> 寫入，
/// 已被其他請求輪替時影響 0 筆、不覆蓋。<c>ExecuteUpdate</c> 不經過攔截器，故自行填入 ModifyDate／ModifyUser。
/// </summary>
public sealed class SessionRotationStore(AppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    : ISessionRotationStore
{
    public async Task<bool> TryCommitRotationAsync(
        UserSession rotated, string expectedTokenHash, CancellationToken cancellationToken)
    {
        var modifyDate = timeProvider.GetLocalNow().DateTime;
        var modifyUser = currentUser.UserName;
        var affected = await db.UserSessions
            .Where(s => s.Id == rotated.Id && s.TokenHash == expectedTokenHash)
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.TokenHash, rotated.TokenHash)
                .SetProperty(s => s.PreviousTokenHash, rotated.PreviousTokenHash)
                .SetProperty(s => s.LastRotatedAt, rotated.LastRotatedAt)
                .SetProperty(s => s.ExpiresAt, rotated.ExpiresAt)
                .SetProperty(s => s.ModifyDate, modifyDate)
                .SetProperty(s => s.ModifyUser, modifyUser),
                cancellationToken);

        var entry = db.Entry(rotated);
        if (affected == 0)
        {
            // 已被其他請求輪替：丟棄記憶體中的輪替，改為資料庫中的最新值。
            await entry.ReloadAsync(cancellationToken);
            return false;
        }

        // 已寫入：讓追蹤狀態與資料庫一致，避免同一請求之後的 SaveChanges 再寫一次。
        entry.Property(s => s.ModifyDate).CurrentValue = modifyDate;
        entry.Property(s => s.ModifyUser).CurrentValue = modifyUser;
        entry.OriginalValues.SetValues(entry.CurrentValues);
        entry.State = EntityState.Unchanged;
        return true;
    }
}
