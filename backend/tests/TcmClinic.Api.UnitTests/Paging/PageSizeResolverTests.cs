using CsCheck;
using TcmClinic.Api.UseCases.Paging;

namespace TcmClinic.Api.UnitTests.Paging;

/// <summary>
/// 分頁清單的每頁筆數決定規則（ADR-007、API-CONVENTIONS §4；SPEC-016 2026-10-06 變更）：
/// 未指定、0 或負數時固定 10；指定正整數時依指定值，上限 100。不讀取系統參數。
/// </summary>
public class PageSizeResolverTests
{
    // 情境：未指定或指定非正數 → 一律為預設 10。
    // @SPEC-016 @AC-14
    [Theory(DisplayName = "未指定、0 或負數時每頁筆數為 10（016，ADR-007）")]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Unspecified_or_non_positive_is_default(int? requested)
    {
        Assert.Equal(10, PageSizeResolver.Resolve(requested));
        Assert.Equal(10, PageSizeResolver.DefaultPageSize);
    }

    // 情境（不變式）：1～100 的指定值原樣採用。
    // @SPEC-016 @AC-16
    [Fact(DisplayName = "指定 1～100 時依指定值（016，ADR-007）")]
    public void Requested_within_max_is_used()
    {
        Gen.Int[1, 100].Sample(n => PageSizeResolver.Resolve(n) == n);
    }

    // 情境（不變式）：超過上限一律以 100 為準。
    // @SPEC-016 @AC-16
    [Fact(DisplayName = "指定超過 100 時以 100 為準（016，API-CONVENTIONS §4）")]
    public void Requested_is_capped_at_max()
    {
        Gen.Int[101, int.MaxValue].Sample(n => PageSizeResolver.Resolve(n) == PageSizeResolver.MaxPageSize);
        Assert.Equal(100, PageSizeResolver.MaxPageSize);
    }
}
