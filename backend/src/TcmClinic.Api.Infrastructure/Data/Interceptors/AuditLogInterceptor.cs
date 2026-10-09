using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Infrastructure.Data.Auditing;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data.Interceptors;

/// <summary>
/// 於存檔時擷取受稽核實體的改前/改後整列快照，寫入 <c>SysAuditLog</c>（SPEC-006）。
/// [DATA_AUDIT] 監控關鍵字（Principle X）。必須登記於 <see cref="AuditableEntityInterceptor"/>
/// 之後：使快照含剛填入的 Modify* 欄，並把硬刪除已轉成的 Modified+Removed=1 正確判為刪除。
/// 因在 AuditableEntityInterceptor 之後才加入，AuditLog 的五個標準欄位由本攔截器經建構子填入（設計決策 R12）。
/// </summary>
/// <remarks>
/// 主鍵由資料庫 IDENTITY 配號（ADR-010），新增實體在存檔前只有暫時鍵，子實體的外鍵也可能指向暫時鍵。
/// 因此分兩段（FR-006、FR-007）：
/// <list type="number">
///   <item>存檔前擷取操作類型與改前快照（存檔後原始值會被覆寫）；若尚無交易，開一個本攔截器自己的交易。</item>
///   <item>存檔後以真實主鍵與外鍵補正改後快照，寫入稽核紀錄，再提交自己開的交易。</item>
/// </list>
/// 存檔或寫入稽核失敗時回復交易，資料與稽核紀錄同成同敗。呼叫端已有交易時沿用它，提交與回復交由呼叫端。
/// </remarks>
public sealed class AuditLogInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    private sealed class PendingAudit
    {
        public List<(EntityEntry Entry, AuditCapture Capture)> Captures { get; } = [];

        public IDbContextTransaction? OwnedTransaction { get; set; }

        public bool WritingLogs { get; set; }
    }

    private readonly ConditionalWeakTable<DbContext, PendingAudit> _pending = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (Capture(eventData.Context) is { } pending && eventData.Context!.Database.CurrentTransaction is null)
        {
            pending.OwnedTransaction = eventData.Context.Database.BeginTransaction();
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (Capture(eventData.Context) is { } pending && eventData.Context!.Database.CurrentTransaction is null)
        {
            pending.OwnedTransaction = await eventData.Context.Database.BeginTransactionAsync(cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is { } context && TakePending(context) is { } pending)
        {
            try
            {
                pending.WritingLogs = true;
                context.AddRange(BuildLogs(pending));
                context.SaveChanges();
                pending.OwnedTransaction?.Commit();
            }
            catch
            {
                pending.OwnedTransaction?.Rollback();
                throw;
            }
            finally
            {
                pending.OwnedTransaction?.Dispose();
            }
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && TakePending(context) is { } pending)
        {
            try
            {
                pending.WritingLogs = true;
                context.AddRange(BuildLogs(pending));
                await context.SaveChangesAsync(cancellationToken);
                if (pending.OwnedTransaction is not null)
                {
                    await pending.OwnedTransaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                if (pending.OwnedTransaction is not null)
                {
                    await pending.OwnedTransaction.RollbackAsync(CancellationToken.None);
                }

                throw;
            }
            finally
            {
                if (pending.OwnedTransaction is not null)
                {
                    await pending.OwnedTransaction.DisposeAsync();
                }
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is { } context && TakePending(context) is { OwnedTransaction: { } transaction })
        {
            transaction.Rollback();
            transaction.Dispose();
        }

        base.SaveChangesFailed(eventData);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && TakePending(context) is { OwnedTransaction: { } transaction })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            await transaction.DisposeAsync();
        }

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <summary>擷取待稽核的異動；寫入稽核紀錄本身的那次存檔不再擷取（遞迴防護）。</summary>
    private PendingAudit? Capture(DbContext? context)
    {
        if (context is null || (_pending.TryGetValue(context, out var current) && current.WritingLogs))
        {
            return null;
        }

        var pending = new PendingAudit();
        foreach (var entry in context.ChangeTracker.Entries().Where(AuditScope.IsAudited).ToList())
        {
            if (AuditSnapshotBuilder.Build(entry) is { } capture)
            {
                pending.Captures.Add((entry, capture)); // no-op（FR-008）不記
            }
        }

        if (pending.Captures.Count == 0)
        {
            return null;
        }

        _pending.AddOrUpdate(context, pending);
        return pending;
    }

    private PendingAudit? TakePending(DbContext context)
    {
        if (!_pending.TryGetValue(context, out var pending) || pending.WritingLogs)
        {
            return null;
        }

        _pending.Remove(context);
        return pending;
    }

    private List<AuditLog> BuildLogs(PendingAudit pending)
    {
        var now = DateTime.Now;
        var user = currentUser.UserName;
        return pending.Captures
            .Select(c => AuditSnapshotBuilder.RefreshAfterSave(c.Capture, c.Entry))
            .Select(c => new AuditLog(c.EntityName, c.TableName, c.PrimaryKey, c.Operation, c.OldValues, c.NewValues, user, now))
            .ToList();
    }
}
