using CsCheck;
using TcmClinic.Api.Core.MenuAggregate;
using Vogen;

namespace TcmClinic.Api.UnitTests.MenuAggregate;

/// <summary>
/// MenuItem 領域不變量的 property-based 測試（Principle XV / CsCheck）。
/// </summary>
public class MenuItemTests
{
    // 情境：長度 1–30 的名稱皆為合法。
    // Invariant：合法名稱建構後 Value 與輸入完全相同（round-trip 保值，FR-007 名稱規則）。
    [Fact(DisplayName = "選單名稱長度 1–30 皆合法且建構後保值（round-trip，FR-007）")]
    public void MenuItemName_accepts_valid_lengths()
    {
        Gen.String[Gen.Char['a', 'z'], 1, 30].Sample(s =>
        {
            var name = MenuItemName.From(s);
            return name.Value == s;
        });
    }

    // 情境：長度 31–60 的名稱過長。
    // Invariant：任何超長名稱都必須被拒（丟出 ValueObjectValidationException，FR-007）。
    [Fact(DisplayName = "選單名稱長度 31–60 過長時必被拒（ValueObjectValidationException，FR-007）")]
    public void MenuItemName_rejects_too_long()
    {
        Gen.String[Gen.Char['a', 'z'], 31, 60].Sample(s =>
        {
            try
            {
                MenuItemName.From(s);
                return false;
            }
            catch (ValueObjectValidationException)
            {
                return true;
            }
        });
    }

    // 情境：以路由建立新選單項 → 預設為啟用、且因有路由而非容器。
    [Fact(DisplayName = "以路由建立選單項時預設啟用且非容器")]
    public void New_menu_item_is_enabled_by_default()
    {
        var item = new MenuItem(MenuItemName.From("使用者"), null, "/admin/users");
        Assert.True(item.IsEnabled);
        Assert.False(item.IsContainer);
    }

    // 情境：不給路由建立選單項 → 應為純容器父項（Route 為 null）。
    // @SPEC-002 @AC-20
    [Fact(DisplayName = "未給路由建立選單項時應為純容器（Route 為 null）")]
    public void Item_without_route_is_container()
    {
        var item = new MenuItem(MenuItemName.From("系統管理"), null);
        Assert.True(item.IsContainer);
        Assert.Null(item.Route);
    }

    // 情境：連續呼叫 SetEnabled/Reorder → 變更方法須為 fluent（回傳同一實例）且確實套用。
    // @SPEC-002 @AC-2
    [Fact(DisplayName = "變更方法為 fluent（回傳同一實例）且確實套用")]
    public void Mutation_methods_are_fluent_and_apply()
    {
        var item = new MenuItem(MenuItemName.From("角色"), null, "/admin/roles");

        var result = item.SetEnabled(false).Reorder(5);

        Assert.Same(item, result);
        Assert.False(item.IsEnabled);
        Assert.Equal(5, item.DisplayOrder);
    }

    // 情境（設計決策 R8）：頂層以 null 表示——子選單移到頂層後 ParentId 為 null，移回父選單後為該父選單 Id。
    // @SPEC-002 @AC-21
    [Fact(DisplayName = "選單以 null 表示頂層，可移到頂層再移回父選單（設計決策 R8）")]
    public void MoveTo_null_moves_item_to_top_level()
    {
        var item = new MenuItem(MenuItemName.From("使用者"), MenuItemId.From(1), "/admin/users");

        Assert.Same(item, item.MoveTo(null));
        Assert.Null(item.ParentId);

        item.MoveTo(MenuItemId.From(2));
        Assert.Equal(MenuItemId.From(2), item.ParentId);
    }

    // 情境：路由僅含空白字元 → 應正規化視為無路由，即純容器。
    // @SPEC-002 @AC-20
    [Fact(DisplayName = "路由僅含空白時正規化為無路由即純容器")]
    public void Blank_route_is_normalized_to_container()
    {
        var item = new MenuItem(MenuItemName.From("群組"), null, "   ");
        Assert.True(item.IsContainer);
    }
}
