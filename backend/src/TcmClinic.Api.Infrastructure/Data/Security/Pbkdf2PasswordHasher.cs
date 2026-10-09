using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.Infrastructure.Data.Security;

/// <summary>
/// 以「每帳號隨機 salt + PBKDF2-HMAC-SHA512」保存密碼（SPEC-004 FR-007/008、ADR-009）；
/// 2026-10-08 前的 SHA-256 雜湊仍可驗證，並由 <see cref="NeedsRehash"/> 標示需在登入時轉換（FR-007a）。
/// </summary>
/*
 * [商業規範 2026-10-08] 密碼雜湊改 PBKDF2（開發規範裁決撤銷 SHA-256 例外，ADR-009）。
 * 1. 參數與 ASP.NET Core Identity v3 預設相同：HMAC-SHA512、100,000 次迭代、16-byte salt、32-byte 輸出；
 *    以 BCL Rfc2898DeriveBytes.Pbkdf2 實作，不新增套件。
 * 2. 雜湊字串為 pbkdf2-sha512$<迭代次數>$<Base64>（65 字元，PasswordHash 欄位 nvarchar(88)）；salt 另存 Base64。
 * 3. 沒有前綴者為舊格式 Base64(SHA256(salt ⧺ UTF8(password)))，仍可驗證以便登入時轉換。
 * 4. 驗證一律固定時間比較（FixedTimeEquals），避免時序側通道；儲存值格式錯誤時回偽而非拋例外。
 */
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "pbkdf2-sha512$";
    private const int Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Derive(password, salt, Iterations);
        return ($"{Prefix}{Iterations.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(hash)}",
            Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            if (!hash.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return CryptographicOperations.FixedTimeEquals(LegacySha256(password, saltBytes), Convert.FromBase64String(hash));
            }

            if (!TryParse(hash, out var iterations, out var expected))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(Derive(password, saltBytes, iterations), expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool NeedsRehash(string hash) =>
        !TryParse(hash, out var iterations, out _) || iterations < Iterations;

    private static bool TryParse(string hash, out int iterations, out byte[] value)
    {
        iterations = 0;
        value = [];
        if (!hash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = hash[Prefix.Length..].Split('$');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
            || iterations <= 0)
        {
            return false;
        }

        var buffer = new byte[parts[1].Length];
        if (!Convert.TryFromBase64String(parts[1], buffer, out var written))
        {
            return false;
        }

        value = buffer[..written];
        return true;
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA512, HashSizeBytes);

    private static byte[] LegacySha256(string password, byte[] salt) =>
        SHA256.HashData([.. salt, .. Encoding.UTF8.GetBytes(password)]);
}
