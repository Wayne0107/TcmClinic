using Ardalis.Specification;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;

namespace TcmClinic.Api.UseCases.Menus;

/// <summary>
/// API 的父選單參數慣例：對外以父選單的對外識別碼（UUIDv7，SPEC-064）表示，<c>null</c> 表示頂層；
/// 領域與資料庫以內部 <see cref="MenuItemId"/> 表示，<c>null</c> 為頂層。於用例入口轉換。
/// （2026-10-08 前對外以 <c>parentId = 0</c> 表示頂層，設計決策 R8。）
/// </summary>
internal static class MenuParent
{
    /// <summary>
    /// 將 API 的父選單參數轉為領域值：<c>null</c> 為頂層；否則依對外識別碼找父選單，找不到時 <c>Found = false</c>。
    /// </summary>
    public static async Task<(bool Found, MenuItemId? ParentId)> ResolveAsync(
        Guid? parentPublicId, IReadRepositoryBase<MenuItem> repository, CancellationToken cancellationToken)
    {
        if (parentPublicId is not { } value)
        {
            return (true, null);
        }

        if (!MenuItemPublicId.TryFrom(value, out var publicId))
        {
            return (false, null);
        }

        var parent = await repository.FirstOrDefaultAsync(new MenuItemByPublicIdSpec(publicId), cancellationToken);
        return parent is null ? (false, null) : (true, parent.Id);
    }
}
