namespace TcmClinic.Api.UseCases.Menus;

/// <summary>選單樹節點（含停用項目）。</summary>
/// <remarks>對外回應用；不外洩領域型別。</remarks>
/// <param name="Id">選單項目對外識別碼（UUIDv7，SPEC-064）。</param>
/// <param name="Name">選單名稱，最長 30 字；同一父層下唯一。</param>
/// <param name="Route">前端選單識別鍵（路由／value），最長 200 字；未設定（如純分組容器）時為 null。</param>
/// <param name="Icon">選單圖示識別字，最長 50 字；未設定時為 null。</param>
/// <param name="DisplayOrder">同層顯示順序，數字小者排前。</param>
/// <param name="IsEnabled">是否啟用；停用的選單不會出現在使用者側邊選單。</param>
/// <param name="Children">子節點，依顯示順序排列；無子節點時為空陣列。</param>
public sealed record MenuTreeDto(
    Guid Id,
    string Name,
    string? Route,
    string? Icon,
    int DisplayOrder,
    bool IsEnabled,
    IReadOnlyList<MenuTreeDto> Children);
