using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Menus;

/// <summary>新增選單項目（FR-001/002/007/019/020）。<c>ParentId</c> 為父選單對外識別碼，<c>null</c> 表示頂層（SPEC-064）。</summary>
public sealed record CreateMenuItemCommand(
    string Name, Guid? ParentId, string? Route, string? Icon, int DisplayOrder)
    : ICommand<Result<Guid>>;

public sealed class CreateMenuItemHandler(IRepository<MenuItem> repository)
    : ICommandHandler<CreateMenuItemCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(
        CreateMenuItemCommand command, CancellationToken cancellationToken)
    {
        var name = MenuItemName.From(command.Name);
        // FR-020：父選單必須存在；先驗證回 400，不讓資料庫外鍵違反變成 500。
        var (parentFound, parentId) = await MenuParent.ResolveAsync(command.ParentId, repository, cancellationToken);
        if (!parentFound)
        {
            return Result<Guid>.Invalid(new ValidationError
            {
                Identifier = nameof(command.ParentId),
                ErrorMessage = "父選單不存在。",
            });
        }

        var duplicate = await repository.AnyAsync(
            new MenuItemByParentAndNameSpec(parentId, name), cancellationToken);
        if (duplicate)
        {
            return Result<Guid>.Invalid(new ValidationError
            {
                Identifier = nameof(command.Name),
                ErrorMessage = "同層選單名稱已存在。",
            });
        }

        var item = new MenuItem(name, parentId, command.Route, command.Icon, command.DisplayOrder);
        await repository.AddAsync(item, cancellationToken);

        return Result<Guid>.Success(item.PublicId.Value);
    }
}
