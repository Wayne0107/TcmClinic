namespace TcmClinic.Api.UseCases.Paging;

/// <summary>
/// 分頁清單的每頁筆數決定規則（ADR-007、API-CONVENTIONS §4）：
/// 有指定正整數時依指定值（上限 <see cref="MaxPageSize"/>）；未指定、0 或負數時為 <see cref="DefaultPageSize"/>。
/// 2026-10-06 前預設值取自系統參數「查詢列表分頁筆數」（SPEC-016），該參數移除後改為固定值。
/// </summary>
public static class PageSizeResolver
{
    /// <summary>未指定每頁筆數時的預設值。</summary>
    public const int DefaultPageSize = 10;

    /// <summary>每頁筆數上限。</summary>
    public const int MaxPageSize = 100;

    public static int Resolve(int? requested) =>
        requested is > 0 ? Math.Min(requested.Value, MaxPageSize) : DefaultPageSize;
}
