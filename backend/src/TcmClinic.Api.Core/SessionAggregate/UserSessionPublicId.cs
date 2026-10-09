using Vogen;

namespace TcmClinic.Api.Core.SessionAggregate;

/// <summary>登入 session 對外識別碼（UUIDv7）：即 access token 的 <c>sid</c> claim（ADR-011），不公開整數主鍵（SPEC-064）。</summary>
[ValueObject<Guid>]
public readonly partial struct UserSessionPublicId
{
    /// <summary>產生新的 UUIDv7 識別碼。</summary>
    public static UserSessionPublicId New() => From(Guid.CreateVersion7());

    private static Validation Validate(Guid value) =>
        value != Guid.Empty ? Validation.Ok : Validation.Invalid("UserSessionPublicId 不得為空值。");
}
