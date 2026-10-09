using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data.Interceptors;

/// <summary>
/// 自動填入稽核欄位（Principle XI）並將實體刪除轉為軟刪除（Removed=1）。
/// 時間採本地時間。應用／用例層不得手動設定這些欄位。
/// </summary>
public sealed class AuditableEntityInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.Now;
        var user = currentUser.UserName;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditable auditable)
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreateDate = now;
                    auditable.CreateUser = user;
                    auditable.ModifyDate = now;
                    auditable.ModifyUser = user;
                    break;
                case EntityState.Modified:
                    auditable.ModifyDate = now;
                    auditable.ModifyUser = user;
                    break;
                case EntityState.Deleted when entry.Entity is ISoftDelete softDelete:
                    entry.State = EntityState.Modified;
                    softDelete.Removed = true;
                    auditable.ModifyDate = now;
                    auditable.ModifyUser = user;
                    break;
                default:
                    break;
            }
        }
    }
}
