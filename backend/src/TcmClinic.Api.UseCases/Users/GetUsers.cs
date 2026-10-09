using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Paging;

namespace TcmClinic.Api.UseCases.Users;

/// <summary>使用者清單中的一筆使用者；不含密碼資訊。</summary>
/// <param name="Id">使用者對外識別碼（UUIDv7，SPEC-064）。</param>
/// <param name="Account">登入帳號（小寫）。</param>
/// <param name="DisplayName">顯示名稱。</param>
/// <param name="RoleCodes">所屬角色代碼清單，與 RoleNames 依序一一對應。</param>
/// <param name="RoleNames">所屬角色名稱清單，與 RoleCodes 依序一一對應。</param>
public sealed record UserListItem(
    Guid Id,
    string Account,
    string DisplayName,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> RoleNames);

/// <summary>使用者分頁清單（僅含未刪除的使用者）。</summary>
/// <param name="Items">本頁使用者，依帳號遞增排序；無資料時為空陣列。</param>
/// <param name="Page">實際套用的頁碼（從 1 起算）。</param>
/// <param name="PageSize">實際套用的每頁筆數（1～100）。</param>
/// <param name="TotalCount">符合條件的使用者總筆數（不受分頁影響）。</param>
public sealed record UserListResult(
    IReadOnlyList<UserListItem> Items, int Page, int PageSize, int TotalCount);

/// <summary>查詢使用者清單（US2，FR-010）。分頁 + 關鍵字（帳號／名稱），僅未刪除；不含密碼。</summary>
public sealed record GetUsersQuery(string? Keyword, int Page, int? PageSize)
    : IQuery<Result<UserListResult>>;

public sealed class GetUsersHandler(
    IReadRepository<User> userRepository,
    IReadRepository<Role> roleRepository)
    : IQueryHandler<GetUsersQuery, Result<UserListResult>>
{
    public async ValueTask<Result<UserListResult>> Handle(
        GetUsersQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        // 未指定、0 或負數時每頁 10 筆，指定時上限 100（ADR-007）。
        var pageSize = PageSizeResolver.Resolve(query.PageSize);

        // 後台管理者規模有限：載入未刪除使用者後於記憶體過濾/分頁，避開 Vogen 轉換屬性的 LIKE 翻譯限制。
        var users = await userRepository.ListAsync(new UsersOrderedSpec(), cancellationToken);
        var roles = await roleRepository.ListAsync(new RolesOrderedSpec(), cancellationToken);
        var roleMap = roles.ToDictionary(r => r.Id, r => (Code: r.Code.Value, Name: r.Name.Value));

        var keyword = query.Keyword?.Trim().ToLowerInvariant();
        var filtered = string.IsNullOrEmpty(keyword)
            ? users
            : users.Where(u =>
                u.Account.Value.Contains(keyword)
                || u.DisplayName.Value.ToLowerInvariant().Contains(keyword)).ToList();

        var total = filtered.Count;

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u =>
            {
                var codes = new List<string>();
                var names = new List<string>();
                foreach (var ur in u.UserRoles)
                {
                    if (roleMap.TryGetValue(ur.RoleId, out var role))
                    {
                        codes.Add(role.Code);
                        names.Add(role.Name);
                    }
                }
                return new UserListItem(u.PublicId.Value, u.Account.Value, u.DisplayName.Value, codes, names);
            })
            .ToList();

        return Result<UserListResult>.Success(new UserListResult(items, page, pageSize, total));
    }
}
