using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Menus;

/// <summary>調整同一父層下的顯示順序（FR-005）。識別碼皆為對外識別碼，<c>ParentId = null</c> 表示頂層（SPEC-064）。</summary>
public sealed record ReorderMenuItemsCommand(Guid? ParentId, IReadOnlyList<Guid> OrderedIds)
    : ICommand<Result>;

public sealed class ReorderMenuItemsHandler(IRepository<MenuItem> repository)
    : ICommandHandler<ReorderMenuItemsCommand, Result>
{
    public async ValueTask<Result> Handle(
        ReorderMenuItemsCommand command, CancellationToken cancellationToken)
    {
        var (parentFound, parentId) = await MenuParent.ResolveAsync(command.ParentId, repository, cancellationToken);
        if (!parentFound)
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(command.ParentId),
                ErrorMessage = "父選單不存在。",
            });
        }

        var items = await repository.ListAsync(new MenuItemsByParentSpec(parentId), cancellationToken);

        for (var index = 0; index < command.OrderedIds.Count; index++)
        {
            var publicId = command.OrderedIds[index];
            var item = items.FirstOrDefault(m => m.PublicId.Value == publicId);
            if (item is null)
            {
                return Result.Invalid(new ValidationError
                {
                    ErrorMessage = $"選單項目 {command.OrderedIds[index]} 不屬於此父層。",
                });
            }

            item.Reorder(index);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
