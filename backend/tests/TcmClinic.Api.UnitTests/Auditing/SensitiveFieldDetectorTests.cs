using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UnitTests.Auditing;

/// <summary>
/// SensitiveFieldDetector 純邏輯（SPEC-010 機密護欄，SC-002）：依 @SensitiveData 標記判定「是否涉及敏感資料」
/// 並列出被標記的欄位「名稱」。核心保證：僅回傳欄位名，機密「值」永不經手（FR-005）。
/// </summary>
public class SensitiveFieldDetectorTests
{
    /// <summary>無任何敏感標記的一般型別（對照組）。</summary>
    private sealed class PlainDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    /// <summary>含機密欄位的型別；欄位值刻意設為明碼機密，用以驗證偵測器絕不觸及值。</summary>
    private sealed class CredentialHolder
    {
        public string Account { get; set; } = "alice";

        [SensitiveData]
        public string PasswordHash { get; set; } = "super-secret-hash-value";

        [SensitiveData]
        public string ApiToken { get; set; } = "tok_live_do_not_leak";
    }

    // 情境：User 實體含 PasswordHash/PasswordSalt → 偵測為涉及敏感資料且列出兩欄名。
    // @SPEC-010 @AC-1
    [Fact(DisplayName = "User 型別偵測為涉及敏感資料並列出敏感欄名")]
    public void Detects_user_sensitive_fields()
    {
        Assert.True(SensitiveFieldDetector.InvolvesSensitiveData(typeof(User)));

        var fields = SensitiveFieldDetector.GetSensitiveFieldNames(typeof(User));
        Assert.Contains(nameof(User.PasswordHash), fields);
        Assert.Contains(nameof(User.PasswordSalt), fields);
    }

    // 情境：一般型別無敏感標記 → 不涉及敏感資料、欄名清單為空。
    // @SPEC-010 @AC-3
    [Fact(DisplayName = "一般型別不涉及敏感資料且欄名清單為空")]
    public void Plain_type_has_no_sensitive_fields()
    {
        Assert.False(SensitiveFieldDetector.InvolvesSensitiveData(typeof(PlainDto)));
        Assert.Empty(SensitiveFieldDetector.GetSensitiveFieldNames(typeof(PlainDto)));
    }

    // 情境（護欄核心）：偵測器只回欄位「名」，絕不含機密「值」（SC-002）。
    // @SPEC-010 @AC-4
    [Fact(DisplayName = "偵測結果僅含欄位名、絕不含機密值")]
    public void Returns_field_names_never_values()
    {
        var fields = SensitiveFieldDetector.GetSensitiveFieldNames(typeof(CredentialHolder));

        // 回傳的是屬性名。
        Assert.Equal([nameof(CredentialHolder.PasswordHash), nameof(CredentialHolder.ApiToken)], fields);

        // 機密「值」絕不出現在偵測結果的任一項。
        Assert.DoesNotContain("super-secret-hash-value", fields);
        Assert.DoesNotContain("tok_live_do_not_leak", fields);
        Assert.All(fields, name => Assert.DoesNotContain("secret", name, StringComparison.OrdinalIgnoreCase));
    }
}
