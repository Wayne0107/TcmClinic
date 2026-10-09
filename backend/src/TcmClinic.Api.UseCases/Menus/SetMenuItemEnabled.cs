using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Menus;

/// <summary>啟用／停用選單項目（FR-006）。</summary>
public sealed record SetMenuItemEnabledCommand(Guid Id, bool IsEnabled) : ICommand<Result>;

public sealed class SetMenuItemEnabledHandler(IRepository<MenuItem> repository)
    : ICommandHandler<SetMenuItemEnabledCommand, Result>
{
    public async ValueTask<Result> Handle(
        SetMenuItemEnabledCommand command, CancellationToken cancellationToken)
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

        item.SetEnabled(command.IsEnabled);
        await repository.UpdateAsync(item, cancellationToken);

        return Result.Success();
    }
}
