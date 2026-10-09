using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data;
using Serilog.Core;
using Serilog.Events;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// [維運提示] 捕捉用 Serilog sink：把每個 LogEvent 的訊息與所有屬性值攤平成字串收集，
/// 供測試斷言「機密不入日誌」（Principle XVII）。僅測試用，不隨產物出貨。
/// </summary>
public sealed class CapturingLogSink : ILogEventSink
{
    private readonly List<string> _entries = new();
    private readonly List<LogEvent> _events = new();
    private readonly object _gate = new();

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        logEvent.RenderMessage(writer);
        var text = writer.ToString();
        foreach (var property in logEvent.Properties)
        {
            text += " | " + property.Key + "=" + property.Value.ToString();
        }

        lock (_gate)
        {
            _entries.Add(text);
            _events.Add(logEvent);
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_gate)
        {
            return _entries.ToArray();
        }
    }

    /// <summary>原始 LogEvent 快照，供斷言 TraceId 與請求上下文屬性（LogContext enrich）。</summary>
    public IReadOnlyList<LogEvent> EventSnapshot()
    {
        lock (_gate)
        {
            return _events.ToArray();
        }
    }
}

/// <summary>
/// 真實 Cookie scheme 的功能測試工廠，並在應用程式的 Serilog 設定上追加捕捉用 sink，
/// 以驗證登入流程之日誌不含機密（Principle XVII）。需 Docker Desktop 執行中。
/// </summary>
public class LoggingCaptureWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabase _dbContainer = new();

    public CapturingLogSink Sink { get; } = new();

    public CapturingLoggerProvider Provider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        // 登入限速（FR-006b）放寬：TestServer 連線沒有 IP，所有登入會算成同一來源；限速本身由 LoginRateLimitTests 驗證。
        builder.UseSetting("LoginRateLimit:PermitLimit", "100000");

        // 以 DI 註冊捕捉 sink（LoggerConfigs 經 ReadFrom.Services 納入），應用程式的日誌設定原樣執行，
        // 使請求記錄與 Mediator LoggingBehavior 的事件都被攔截供斷言。刻意不另調等級——
        // 測試須反映正式執行層級（EF Core SQL 指令類別已由設定壓到 Warning，見 LoggingLevelTests）。
        // 另以 DI 註冊捕捉用 ILoggerProvider，代表 OpenTelemetry 日誌提供者可收到的內容（見 LoggingProviderTests）。
        builder.ConfigureServices(services => services
            .AddSingleton<ILogEventSink>(Sink)
            .AddSingleton<ILoggerProvider>(Provider));
    }

    public async ValueTask InitializeAsync() => await _dbContainer.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 日誌捕捉測試集合：共用單一 <see cref="LoggingCaptureWebApplicationFactory"/>，且不與其他測試平行執行。
/// UseSerilog 會改寫全域 Log.Logger，多個測試主機同時執行時，事件可能寫進另一個主機的 sink，故須獨占執行。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LoggingCaptureCollection : ICollectionFixture<LoggingCaptureWebApplicationFactory>
{
    public const string Name = "LoggingCapture";
}

/// <summary>日誌機密不外洩功能測試（Principle XVII 機密遮罩 MUST；FR-009 延伸）。</summary>
[Collection(LoggingCaptureCollection.Name)]
public class LoggingRedactionTests(LoggingCaptureWebApplicationFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境：登入（成功 + 失敗）都攜帶密碼，其流程日誌 MUST NOT 含明文密碼，亦不得含該帳號實際
    // 儲存的密碼雜湊或 salt 值（Principle XVII 機密遮罩、FR-009 延伸）。重設密碼端點共用同一日誌
    // 管線（LoggingBehavior 僅記訊息型別名、請求記錄不含 body），故此護欄同時保障該路徑；
    // 若日後有人把 {@Request} 加進 LoggingBehavior、或對實體開啟敏感資料記錄，本測試會轉紅。
    // @SPEC-004 @AC-23
    [Fact(DisplayName = "登入流程日誌不得含明文密碼或雜湊/salt（Principle XVII、FR-009 延伸）")]
    public async Task Login_flow_logs_never_contain_credentials()
    {
        const string realSecret = "Admin123!";
        const string wrongSecret = "WrongPass9!";
        var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/auth/login", new { account = "admin", password = realSecret }, Ct);
        await client.PostAsJsonAsync(
            "/auth/login", new { account = "admin", password = wrongSecret }, Ct);

        // 取種子超管實際儲存的雜湊/salt 值，作為「不可外洩的機密值」比對基準。
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = (await db.Set<User>().AsNoTracking().ToListAsync(Ct))
            .First(u => u.Account == UserAccount.From("admin"));

        var logs = factory.Sink.Snapshot();

        // 前提健檢：確有捕捉到日誌，避免以下斷言淪為「空集合恆真」。
        Assert.NotEmpty(logs);
        Assert.DoesNotContain(logs, m => m.Contains(realSecret, StringComparison.Ordinal));
        Assert.DoesNotContain(logs, m => m.Contains(wrongSecret, StringComparison.Ordinal));
        Assert.DoesNotContain(logs, m => m.Contains(admin.PasswordHash, StringComparison.Ordinal));
        Assert.DoesNotContain(logs, m => m.Contains(admin.PasswordSalt, StringComparison.Ordinal));
    }

    // 情境（SPEC-013，SC-006）：個人資料維護送出三個密碼欄位（成功與失敗各一次），
    // 應用程式日誌與操作紀錄內容 MUST NOT 含任何一個密碼字串。
    // @SPEC-013 @AC-23
    [Fact(DisplayName = "個人資料維護的密碼欄位不得出現在日誌或操作紀錄（013，SC-006）")]
    public async Task Profile_password_fields_never_logged()
    {
        const string current = "Admin123!";
        const string wrongCurrent = "Leak-Check-Wrong-0Aa";
        const string newPassword = "Leak-Check-New-1Bb";
        var client = factory.CreateClient();
        (await AuthTestSupport.PostLoginAsync(client, "admin", current, Ct)).EnsureSuccessStatusCode();

        // 失敗：目前密碼錯誤（進入用例、寫失敗紀錄）。
        await client.PutAsJsonAsync("/profile", new
        {
            displayName = "系統管理員", currentPassword = wrongCurrent,
            newPassword, confirmNewPassword = newPassword,
        }, Ct);

        // 成功：以正確目前密碼變更。
        var ok = await client.PutAsJsonAsync("/profile", new
        {
            displayName = "系統管理員", currentPassword = current,
            newPassword, confirmNewPassword = newPassword,
        }, Ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, ok.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var contents = await db.UserLogs.AsNoTracking().Select(l => l.Content).ToListAsync(Ct);
        var logs = factory.Sink.Snapshot();

        // 前提健檢：確有日誌與兩筆操作紀錄，避免斷言淪為「空集合恆真」。
        Assert.NotEmpty(logs);
        Assert.Equal(2, contents.Count);
        foreach (var secret in new[] { current, wrongCurrent, newPassword })
        {
            Assert.DoesNotContain(logs, m => m.Contains(secret, StringComparison.Ordinal));
            Assert.DoesNotContain(contents, c => c.Contains(secret, StringComparison.Ordinal));
        }
    }
}
