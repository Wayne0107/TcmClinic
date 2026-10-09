using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.RoleAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Roles;

/// <summary>
/// 設定某角色的可見選單（覆寫）。僅接受「葉節點（無子節點）」，拒絕容器 id（US2/US3, FR-009/010/011）。
/// 容器判定對齊前端選單：以「是否有子節點」而非 Route is null（業務選單容器的 Route 非 null）；導覽側仍維持 005 之 Route 判定。
/// 012：<see cref="RowVersion"/> 必填——以樂觀併發權杖比對，防止兩位管理員同時設定時一方變更被靜默覆蓋（FR-015）。
/// </summary>
public sealed record SetRoleMenuVisibilityCommand(
    Guid RoleId, IReadOnlyList<Guid> GrantedMenuItemIds, string? RowVersion)
    : ICommand<Result>;

public sealed class SetRoleMenuVisibilityHandler(
    IRepository<Role> roleRepository,
    IReadRepository<MenuItem> menuRepository,
    IRepository<RoleMenuVisibility> visibilityRepository)
    : ICommandHandler<SetRoleMenuVisibilityCommand, Result>
{
    public async ValueTask<Result> Handle(
        SetRoleMenuVisibilityCommand command, CancellationToken cancellationToken)
    {
        // 並行權杖必填（012，FR-015）：缺→400。
        if (string.IsNullOrWhiteSpace(command.RowVersion))
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(command.RowVersion),
                ErrorMessage = "缺少並行權杖（rowVersion）；請重新載入後再儲存。",
            });
        }

        if (!RolePublicId.TryFrom(command.RoleId, out var publicId))
        {
            return Result.NotFound();
        }

        var role = await roleRepository.FirstOrDefaultAsync(new RoleByPublicIdSpec(publicId), cancellationToken);
        if (role is null)
        {
            return Result.NotFound();
        }

        var roleId = role.Id;

        // 樂觀併發比對：客戶端載入後若已被他人變更，版本不符→409（FR-015 第一道防線）。
        if (!string.Equals(Convert.ToBase64String(role.RowVersion), command.RowVersion, StringComparison.Ordinal))
        {
            return Result.Conflict("資料已被他人變更，請重新載入後再儲存。");
        }

        var items = await menuRepository.ListAsync(new AllMenuItemsOrderedSpec(), cancellationToken);
        var itemsById = items.ToDictionary(m => m.Id.Value);

        // 容器＝具子節點者（以 ParentId 判定），對齊前端選單；不以 Route is null 判定，因業務選單容器的 Route 非 null。
        var containerIds = items
            .Where(m => m.ParentId is not null)
            .Select(m => m.ParentId!.Value.Value)
            .ToHashSet();

        // 請求帶的是選單對外識別碼（SPEC-064），先轉為內部 Id；查無對應者視為不存在的項目。
        var idByPublicId = items.ToDictionary(m => m.PublicId.Value, m => m.Id.Value);
        var requestedPublicIds = command.GrantedMenuItemIds.Distinct().ToList();
        var requested = requestedPublicIds
            .Where(idByPublicId.ContainsKey)
            .Select(id => idByPublicId[id])
            .ToList();

        // 僅允許「存在且為葉節點（無子節點）」的選單項目（FR-010/011）。
        var invalid = requested
            .Where(id => !itemsById.ContainsKey(id) || containerIds.Contains(id))
            .ToList();
        if (invalid.Count > 0 || requested.Count != requestedPublicIds.Count)
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(command.GrantedMenuItemIds),
                ErrorMessage = "只能授權葉節點選單子項（不可含容器或不存在的項目）。",
            });
        }

        var existing = await visibilityRepository.ListAsync(
            new RoleMenuVisibilityByRoleSpec(roleId), cancellationToken);
        var existingByMenu = existing.ToDictionary(v => v.MenuItemId.Value);
        var requestedSet = requested.ToHashSet();

        var toAdd = requested
            .Where(id => !existingByMenu.ContainsKey(id))
            .Select(id => new RoleMenuVisibility(roleId, MenuItemId.From(id)))
            .ToList();

        var toRemove = existing
            .Where(v => !requestedSet.Contains(v.MenuItemId.Value))
            .ToList();

        if (toAdd.Count > 0)
        {
            await visibilityRepository.AddRangeAsync(toAdd, cancellationToken);
        }

        if (toRemove.Count > 0)
        {
            await visibilityRepository.DeleteRangeAsync(toRemove, cancellationToken);
        }

        // 「觸碰」角色以更新並行權杖（rowversion 於下次載入即改變）；極短競態窗內若已被他人提交，
        // SaveChanges 擲 DbUpdateConcurrencyException，由 Web 層對映為 409（FR-015 第二道防線）。
        await roleRepository.UpdateAsync(role, cancellationToken);

        return Result.Success();
    }
}
