namespace TcmClinic.Api.UseCases.Profile;

/// <summary>登入者本人的角色（SPEC-013，FR-001）。</summary>
public sealed record MyProfileRoleDto(string Code, string Name);

/// <summary>
/// 登入者本人的個人資料（SPEC-013，US1、US4；API 契約 §1）。不含任何密碼、雜湊或 salt。
/// </summary>
public sealed record MyProfileDto(
    string Account,
    string DisplayName,
    IReadOnlyList<MyProfileRoleDto> Roles,
    string? Phone,
    string? Email,
    DateOnly PasswordChangedAt,
    int PasswordRemainingDays,
    bool PasswordExpired,
    string Status);
