using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Menus;

/// <summary>刪除（軟刪除）選單項目；有未刪除子項則阻擋（FR-004）。</summary>
public sealed record DeleteMenuItemCommand(Guid Id) : ICommand<Result>;

public sealed class DeleteMenuItemHandler(IRepository<MenuItem> repository)
    : ICommandHandler<DeleteMenuItemCommand, Result>
{
    public async ValueTask<Result> Handle(
        DeleteMenuItemCommand command, CancellationToken cancellationToken)
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

        var hasChildren = await repository.AnyAsync(
            new MenuItemsByParentSpec(item.Id), cancellationToken);
        if (hasChildren)
        {
            return Result.Invalid(new ValidationError
            {
                ErrorMessage = "此選單仍有子項，請先移除或改掛子項。",
            });
        }

        await repository.DeleteAsync(item, cancellationToken); // 攔截器轉為軟刪除 Removed=1
        return Result.Success();
    }
}
