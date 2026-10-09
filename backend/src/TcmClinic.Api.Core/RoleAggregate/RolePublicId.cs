using Vogen;

namespace TcmClinic.Api.Core.RoleAggregate;

/// <summary>角色對外識別碼（UUIDv7）：API 一律以此識別角色，不公開整數主鍵（SPEC-064）。</summary>
[ValueObject<Guid>]
public readonly partial struct RolePublicId
{
    /// <summary>產生新的 UUIDv7 識別碼（時間排序、不可推測，不需查資料庫）。</summary>
    public static RolePublicId New() => From(Guid.CreateVersion7());

    private static Validation Validate(Guid value) =>
        value != Guid.Empty ? Validation.Ok : Validation.Invalid("RolePublicId 不得為空值。");
}
