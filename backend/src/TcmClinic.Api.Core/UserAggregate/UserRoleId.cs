using Vogen;

namespace TcmClinic.Api.Core.UserAggregate;

[ValueObject<int>]
public readonly partial struct UserRoleId
{
    // 不限制正負：新實體存檔前 EF 指派負數暫時值，再由資料庫 IDENTITY 配號（ADR-010）。
    private static Validation Validate(int value) => Validation.Ok;
}
