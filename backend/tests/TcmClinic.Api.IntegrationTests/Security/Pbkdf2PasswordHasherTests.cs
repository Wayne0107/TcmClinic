using System.Security.Cryptography;
using System.Text;
using CsCheck;
using TcmClinic.Api.Infrastructure.Data.Security;

namespace TcmClinic.Api.IntegrationTests.Security;

/// <summary>
/// 加鹽 PBKDF2 密碼雜湊（FR-007/008、FR-007a、ADR-009）：往返一致、不誤放、每次獨立 salt、
/// 新格式字串，以及 2026-10-08 前的 SHA-256 雜湊仍可驗證並標示需要轉換。
/// </summary>
public class Pbkdf2PasswordHasherTests
{
    // PBKDF2 為刻意放慢的雜湊，屬性測試取較少樣本以控制執行時間。
    private const int Samples = 20;

    private readonly Pbkdf2PasswordHasher _hasher = new();

    // 情境：任意密碼雜湊後以正確密碼驗證恆為真（往返一致，FR-008）。
    // @SPEC-004 @AC-13
    [Fact(DisplayName = "任意密碼雜湊後以正確密碼驗證恆為真（往返一致不變量，FR-008）")]
    public void Verify_true_for_correct_password()
    {
        Gen.String[Gen.Char.AlphaNumeric, 1, 40].Sample(password =>
        {
            var (hash, salt) = _hasher.Hash(password);
            return _hasher.Verify(password, hash, salt);
        }, iter: Samples);
    }

    // 情境：以不同密碼驗證恆為偽（不誤放，FR-008）。
    // @SPEC-004 @AC-13
    [Fact(DisplayName = "以不同密碼驗證恆為偽不誤放（不變量，FR-008）")]
    public void Verify_false_for_wrong_password()
    {
        Gen.Select(Gen.String[Gen.Char.AlphaNumeric, 1, 20], Gen.String[Gen.Char.AlphaNumeric, 1, 20])
            .Sample((a, b) =>
            {
                var (hash, salt) = _hasher.Hash(a);
                return a == b || !_hasher.Verify(b, hash, salt);
            }, iter: Samples);
    }

    // 情境：同一密碼兩次雜湊產生相異 salt（每帳號獨立 salt，FR-008）。
    // @SPEC-004 @AC-13
    [Fact(DisplayName = "同一密碼兩次雜湊產生相異 salt（每帳號獨立 salt，FR-008）")]
    public void Salt_is_random_per_hash()
    {
        var (_, salt1) = _hasher.Hash("SamePassword1");
        var (_, salt2) = _hasher.Hash("SamePassword1");
        Assert.NotEqual(salt1, salt2);
    }

    // 情境：新雜湊為 pbkdf2-sha512$100000$<Base64> 格式，長度不超過 PasswordHash 欄位的 88 字元（FR-007）。
    // @SPEC-004 @AC-13
    [Fact(DisplayName = "新雜湊為 pbkdf2-sha512 格式且不超過欄位長度（FR-007）")]
    public void Hash_uses_pbkdf2_format()
    {
        var (hash, salt) = _hasher.Hash("SamePassword1");

        Assert.StartsWith("pbkdf2-sha512$100000$", hash, StringComparison.Ordinal);
        Assert.True(hash.Length <= 88, $"雜湊長度 {hash.Length} 超過欄位上限 88");
        Assert.Equal(32, Convert.FromBase64String(hash["pbkdf2-sha512$100000$".Length..]).Length);
        Assert.Equal(16, Convert.FromBase64String(salt).Length);
    }

    // 情境：2026-10-08 前以 SHA-256(salt⧺password) 保存的雜湊，轉換前仍可驗證（正確密碼為真、錯誤為偽）。
    // @SPEC-004 @AC-13
    [Fact(DisplayName = "舊 SHA-256 雜湊仍可驗證（FR-007a 轉換前）")]
    public void Verifies_legacy_sha256_hash()
    {
        var (legacyHash, salt) = LegacySha256("OldPassword1");

        Assert.True(_hasher.Verify("OldPassword1", legacyHash, salt));
        Assert.False(_hasher.Verify("WrongPassword1", legacyHash, salt));
    }

    // 情境：只有舊格式或迭代次數低於現行值的雜湊需要轉換；現行格式不重寫（FR-007a）。
    // @SPEC-004 @AC-13
    [Fact(DisplayName = "只有舊格式或迭代次數較低的雜湊需要重新雜湊（FR-007a）")]
    public void Needs_rehash_only_for_legacy_or_weaker_hash()
    {
        var (current, _) = _hasher.Hash("SamePassword1");
        var (legacy, _) = LegacySha256("SamePassword1");
        var weaker = "pbkdf2-sha512$10000$" + current["pbkdf2-sha512$100000$".Length..];

        Assert.False(_hasher.NeedsRehash(current));
        Assert.True(_hasher.NeedsRehash(legacy));
        Assert.True(_hasher.NeedsRehash(weaker));
        Assert.True(_hasher.NeedsRehash("pbkdf2-sha512$100000$not-base64!!"));
    }

    // 情境：無效的 Base64 雜湊/salt 或格式錯誤的新格式字串，驗證回偽而非拋例外（穩健性）。
    [Theory(DisplayName = "格式錯誤的雜湊或 salt 驗證回偽而非拋例外（穩健性）")]
    [InlineData("not-base64!!", "also-bad!!")]
    [InlineData("pbkdf2-sha512$abc$AAAA", "AAAAAAAAAAAAAAAAAAAAAA==")]
    [InlineData("pbkdf2-sha512$100000$not-base64!!", "AAAAAAAAAAAAAAAAAAAAAA==")]
    public void Verify_false_for_malformed_stored_values(string hash, string salt)
    {
        Assert.False(_hasher.Verify("pw", hash, salt));
    }

    /// <summary>以 2026-10-08 前的演算法產生雜湊：Base64(SHA256(salt ⧺ UTF8(password)))，salt 16 bytes。</summary>
    private static (string Hash, string Salt) LegacySha256(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = SHA256.HashData([.. salt, .. Encoding.UTF8.GetBytes(password)]);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }
}
