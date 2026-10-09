using Ardalis.Specification;

namespace TcmClinic.Api.Core.SessionAggregate.Specifications;

/// <summary>依 refresh token 雜湊取 session：比對目前值或前一個值（重用偵測，FR-021b）。</summary>
public sealed class UserSessionByTokenHashSpec : Specification<UserSession>
{
    public UserSessionByTokenHashSpec(string tokenHash)
    {
        Query.Where(s => s.TokenHash == tokenHash || s.PreviousTokenHash == tokenHash)
            .TagWith(nameof(UserSessionByTokenHashSpec));
    }
}

/// <summary>依對外識別碼（access token 的 sid）取 session（不追蹤）。供每請求驗證 session 有效（FR-021c）。</summary>
public sealed class UserSessionByPublicIdForReadSpec : Specification<UserSession>
{
    public UserSessionByPublicIdForReadSpec(UserSessionPublicId publicId)
    {
        Query.Where(s => s.PublicId == publicId).AsNoTracking().TagWith(nameof(UserSessionByPublicIdForReadSpec));
    }
}

/// <summary>依對外識別碼（access token 的 sid）取 session（追蹤）。供登出撤銷。</summary>
public sealed class UserSessionByPublicIdSpec : Specification<UserSession>
{
    public UserSessionByPublicIdSpec(UserSessionPublicId publicId)
    {
        Query.Where(s => s.PublicId == publicId).TagWith(nameof(UserSessionByPublicIdSpec));
    }
}
