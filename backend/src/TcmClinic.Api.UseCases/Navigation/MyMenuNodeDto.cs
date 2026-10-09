using System.Text.Json.Serialization;

namespace TcmClinic.Api.UseCases.Navigation;

/// <summary>
/// 目前使用者可見選單樹的節點；葉節點不含 subMenu 鍵。
/// </summary>
/// <remarks>
/// SPEC-008。對齊前端 <c>useMenu.ts</c> 之 <c>MenuItem</c> 型別
/// （<c>{ text, value, subMenu? }</c>）：<c>Text</c>=名稱、<c>Value</c>=前端識別鍵（後端 Route，SPEC-007 存入之 value）、
/// <c>SubMenu</c>=子節點。**葉節點省略 <c>subMenu</c> 鍵**（<c>SubMenu</c> 為 null 時不序列化）。
/// 來源：Text←MenuItem.Name、Value←MenuItem.Route。
/// </remarks>
/// <param name="Text">選單顯示名稱。</param>
/// <param name="Value">前端選單識別鍵（路由／value）；未設定識別鍵的純容器為 null。</param>
/// <param name="SubMenu">子節點，依顯示順序排列；葉節點無此鍵（不輸出，非空陣列也非 null）。</param>
public sealed record MyMenuNodeDto(
    string Text,
    string? Value,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<MyMenuNodeDto>? SubMenu);
