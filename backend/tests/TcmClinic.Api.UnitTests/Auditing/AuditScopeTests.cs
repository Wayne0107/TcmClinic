using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Infrastructure.Data.Auditing;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UnitTests.Auditing;

/// <summary>
/// AuditScope 範圍判定（FR-014）：預設全覆蓋、排除稽核表本身與 [NotAudited] 標記之實體。
/// </summary>
public class AuditScopeTests
{
    [NotAudited]
    private sealed class ExcludedDummy;

    private sealed class IncludedDummy;

    // 情境：一般實體型別 → 受稽核。
    // @SPEC-006 @AC-9
    [Fact(DisplayName = "一般實體型別受稽核")]
    public void Ordinary_entity_is_audited()
    {
        Assert.True(AuditScope.IsAuditedType(typeof(User)));
        Assert.True(AuditScope.IsAuditedType(typeof(IncludedDummy)));
    }

    // 情境：稽核表本身 → 排除（避免遞迴）。
    // @SPEC-006 @AC-9
    [Fact(DisplayName = "AuditLog 本身不受稽核（防遞迴）")]
    public void AuditLog_itself_is_excluded()
        => Assert.False(AuditScope.IsAuditedType(typeof(AuditLog)));

    // 情境：標記 [NotAudited] 的實體 → 排除（排除清單）。
    // @SPEC-006 @AC-9
    [Fact(DisplayName = "標記 NotAudited 的實體被排除")]
    public void NotAudited_entity_is_excluded()
        => Assert.False(AuditScope.IsAuditedType(typeof(ExcludedDummy)));
}
