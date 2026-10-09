using Vogen;

namespace TcmClinic.Api.Core.MenuAggregate;

[ValueObject<int>]
public readonly partial struct MenuItemId
{
    // 不限制正負：0 為 root（無父項）哨兵；新實體存檔前 EF 指派負數暫時值，再由資料庫 IDENTITY 配號（ADR-010）。
    private static Validation Validate(int value) => Validation.Ok;
}
