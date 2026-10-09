using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using TcmClinic.Api.Infrastructure.Data.Auditing;
using TcmClinic.Api.Infrastructure.Data.Interceptors;
using TcmClinic.Api.Infrastructure.Data.Security;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.IntegrationTests.Auditing;

/// <summary>
/// SPEC-013 操作紀錄（FR-015、設計決策 R8）：獨立 DbContext 寫入、不帶出主交易的髒資料、
/// 寫入失敗不拋出（盡力寫入）、SysUserLog 本身不產生 SysAuditLog。
/// </summary>
public class UserLogRecorderTests : IAsyncLifetime
{
    private readonly TestDatabase _db = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>測試用操作者：固定帳號與來源 IP。</summary>
    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string UserName => "alice";

        public string? SourceIp => "10.0.0.1";
    }

    /// <summary>收集錯誤日誌的測試用 logger。</summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                Errors.Add(formatter(state, exception));
            }
        }
    }

    /// <summary>模擬資料庫寫入失敗：只要本次儲存含 UserLog 即拋出例外。</summary>
    private sealed class FailOnUserLogInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<UserLog>().Any())
            {
                throw new InvalidOperationException("模擬資料庫寫入失敗");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();
        await using var provider = BuildProvider(new CapturingLogger<UserLogRecorder>());
        await using var scope = provider.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ctx.Database.MigrateAsync(Ct);
        await SeedData.SeedAsync(ctx, new Pbkdf2PasswordHasher(), "Admin123!", Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private ServiceProvider BuildProvider(CapturingLogger<UserLogRecorder> logger, bool failOnUserLog = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser, FakeCurrentUser>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AuditLogInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(_db.GetConnectionString());
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<AuditLogInterceptor>());
            if (failOnUserLog)
            {
                options.AddInterceptors(new FailOnUserLogInterceptor());
            }
        });
        services.AddSingleton<ILogger<UserLogRecorder>>(logger);
        services.AddScoped<IUserLogRecorder, UserLogRecorder>();
        return services.BuildServiceProvider();
    }

    private static UserLogRequest Request(UserLogStatus status, string content) =>
        new("PersonalData", "Modify", status, content);

    // 情境：記錄一筆成功操作。操作者與時間以五個標準欄位保存（DATABASE-SCHEMA §1、§6；設計決策 R8 2026-09-30 修訂）：
    // CreateUser 取自目前使用者、ModifyDate／ModifyUser 與 Create* 同值（列不會被更新）、Removed 恆為 false；IP 取自目前使用者。
    // @SPEC-013 @AC-24
    [Fact(DisplayName = "操作紀錄以五個標準欄位記錄操作者與時間，IP 取自目前使用者（FR-015、設計決策 R8）")]
    public async Task Records_row_with_current_user()
    {
        await using var provider = BuildProvider(new CapturingLogger<UserLogRecorder>());
        await using (var scope = provider.CreateAsyncScope())
        {
            var recorder = scope.ServiceProvider.GetRequiredService<IUserLogRecorder>();
            await recorder.RecordAsync(Request(UserLogStatus.Success, "成功-紀錄-001"), Ct);
        }

        await using var verifyScope = provider.CreateAsyncScope();
        var ctx = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await ctx.UserLogs.SingleAsync(l => l.Content == "成功-紀錄-001", Ct);
        Assert.Equal("PersonalData", log.FunctionId);
        Assert.Equal("Modify", log.Action);
        Assert.Equal("Success", log.Status);
        Assert.Equal("10.0.0.1", log.ClientIp);
        Assert.Equal("alice", log.CreateUser);
        Assert.NotEqual(default, log.CreateDate);
        Assert.Equal(log.CreateUser, log.ModifyUser);
        Assert.Equal(log.CreateDate, log.ModifyDate);
        Assert.False(log.Removed);

        // SysUserLog 標記 [NotAudited]：寫入不得再產生資料異動紀錄。
        Assert.False(await ctx.AuditLogs.AnyAsync(a => a.EntityName == nameof(UserLog), Ct));
    }

    // 情境：主交易的 DbContext 尚有未儲存的修改時記錄失敗 → 紀錄落庫，但主交易的修改不得被一併寫入。
    // @SPEC-013 @AC-24
    [Fact(DisplayName = "記錄失敗紀錄時不會帶出主交易未儲存的修改（設計決策 R8）")]
    public async Task Failure_log_does_not_flush_pending_changes()
    {
        await using var provider = BuildProvider(new CapturingLogger<UserLogRecorder>());
        await using (var scope = provider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = await ctx.Users.SingleAsync(u => u.Account == UserAccount.From("admin"), Ct);
            admin.Rename(UserName.From("未儲存的名稱"));

            var recorder = scope.ServiceProvider.GetRequiredService<IUserLogRecorder>();
            await recorder.RecordAsync(Request(UserLogStatus.Failure, "失敗-紀錄-002"), Ct);
        }

        await using var verifyScope = provider.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await verify.UserLogs.SingleAsync(l => l.Content == "失敗-紀錄-002", Ct);
        Assert.Equal("Failure", log.Status);
        var admin2 = await verify.Users.AsNoTracking().SingleAsync(u => u.Account == UserAccount.From("admin"), Ct);
        Assert.NotEqual("未儲存的名稱", admin2.DisplayName.Value);
    }

    // 情境：寫入時資料庫發生例外 → 不拋出（不拖垮主流程），並留下 [USER_LOG] 錯誤日誌。
    // @SPEC-013 @AC-24
    [Fact(DisplayName = "操作紀錄寫入失敗時不拋例外並留下錯誤日誌（盡力寫入）")]
    public async Task Write_failure_is_swallowed_and_logged()
    {
        var logger = new CapturingLogger<UserLogRecorder>();
        await using var provider = BuildProvider(logger, failOnUserLog: true);
        await using var scope = provider.CreateAsyncScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IUserLogRecorder>();

        await recorder.RecordAsync(Request(UserLogStatus.Failure, "失敗-紀錄-003"), Ct);

        Assert.Contains(logger.Errors, e => e.Contains("[USER_LOG]"));
    }
}
