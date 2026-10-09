using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Menus;

/// <summary>編輯選單項目（FR-003）。</summary>
public sealed record UpdateMenuItemCommand(Guid Id, string Name, string? Route, string? Icon)
    : ICommand<Result>;

public sealed class UpdateMenuItemHandler(IRepository<MenuItem> repository)
    : ICommandHandler<UpdateMenuItemCommand, Result>
{
    public async ValueTask<Result> Handle(
        UpdateMenuItemCommand command, CancellationToken cancellationToken)
    {
        if (!MenuItemPublicId.TryFrom(command.Id, out var publicId))
        {
            return Result.NotFound();
        }

        var item = await repository.FirstOrDefaultAsync(
            new MenuItemByPublicIdSpec(publicId), cancellationToken);
        if (item is null)
        {
            return Result.NotFound();
        }

        var name = MenuItemName.From(command.Name);

        var sameLevel = await repository.ListAsync(
            new MenuItemByParentAndNameSpec(item.ParentId, name), cancellationToken);
        if (sameLevel.Any(m => m.Id != item.Id))
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(command.Name),
                ErrorMessage = "同層選單名稱已存在。",
            });
        }

        item.UpdateDetails(name, command.Route, command.Icon);
        await repository.UpdateAsync(item, cancellationToken);

        return Result.Success();
    }
}
