using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TcmClinic.Api.Web.Configurations;

namespace TcmClinic.Api.Web.Services;

/// <summary>
/// 簽發 access token（SPEC-004 FR-021，ADR-011）：HMAC-SHA256、預設 15 分鐘；claims 只放身分（sub、name、sid、jti），
/// 角色與可見選單不放進 token，每請求由資料庫取得（FR-021c）。
/// </summary>
public sealed class AccessTokenIssuer(JwtSigningKey signingKey, IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    /// <summary>簽發 access token，回傳 token 與效期秒數。</summary>
    public (string Token, int ExpiresIn) Issue(string account, Guid userPublicId, Guid sessionId)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromMinutes(settings.AccessTokenMinutes);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now + lifetime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userPublicId.ToString(),
                [JwtRegisteredClaimNames.Name] = account,
                [JwtRegisteredClaimNames.Sid] = sessionId.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
            },
            SigningCredentials = new SigningCredentials(signingKey.Key, SecurityAlgorithms.HmacSha256),
        });
        return (token, (int)lifetime.TotalSeconds);
    }
}
