using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace TcmClinic.Api.Core.SessionAggregate;

/// <summary>refresh token 的產生與雜湊（SPEC-004 FR-021，ADR-011）：只把雜湊存進資料庫，原值只交給用戶端。</summary>
public static class RefreshTokenValue
{
    /// <summary>產生 32 bytes 隨機值（Base64Url）與其 SHA-256 雜湊（64 字元小寫十六進位）。</summary>
    public static (string Token, string Hash) Generate()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    /// <summary>計算 refresh token 的 SHA-256 雜湊（64 字元小寫十六進位）。</summary>
    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
