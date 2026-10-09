using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TcmClinic.Api.Web.Configurations;

/// <summary>JWT 設定（設定區段 <c>Jwt</c>；SPEC-004 FR-021，ADR-011）。</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>簽章金鑰最少位元組數（HMAC-SHA256）。</summary>
    public const int MinKeyBytes = 32;

    /// <summary>
    /// HMAC-SHA256 簽章金鑰（Base64，至少 32 bytes）。非 Development 環境必須設定（以環境變數或機密設定提供，不寫進版控）；
    /// Development 未設定時於啟動產生隨機金鑰。
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>發行者（<c>iss</c>）。</summary>
    [Required]
    public string Issuer { get; set; } = "TcmClinic.Api";

    /// <summary>對象（<c>aud</c>）。</summary>
    [Required]
    public string Audience { get; set; } = "TcmClinic.Web";

    /// <summary>access token 效期（分鐘，預設 15）。</summary>
    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>金鑰是否為合法 Base64 且長度足夠。</summary>
    public static bool IsValidKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var buffer = new byte[key.Length];
        return Convert.TryFromBase64String(key, buffer, out var length) && length >= MinKeyBytes;
    }
}

/// <summary>
/// 簽章金鑰（singleton）：取設定的 <see cref="JwtOptions.SigningKey"/>；未設定時（僅 Development 允許，見 <see cref="AuthConfigs"/>
/// 的啟動驗證）產生隨機金鑰——重啟後舊 access token 失效，前端以 refresh token 換新即可。
/// </summary>
public sealed class JwtSigningKey
{
    public JwtSigningKey(IOptions<JwtOptions> options, ILogger<JwtSigningKey> logger)
    {
        var configured = options.Value.SigningKey;
        if (JwtOptions.IsValidKey(configured))
        {
            Key = new SymmetricSecurityKey(Convert.FromBase64String(configured!));
            return;
        }

        logger.LogInformation("[JWT] 未設定 Jwt:SigningKey，使用啟動時產生的隨機簽章金鑰（僅開發環境）");
        Key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(JwtOptions.MinKeyBytes));
    }

    public SymmetricSecurityKey Key { get; }
}
