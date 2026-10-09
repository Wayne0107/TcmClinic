using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.MenuAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Menus;

/// <summary>取得完整選單樹（含停用者，供管理檢視）。</summary>
public sealed record GetMenuTreeQuery : IQuery<Result<IReadOnlyList<MenuTreeDto>>>;

public sealed class GetMenuTreeHandler(IReadRepository<MenuItem> repository)
    : IQueryHandler<GetMenuTreeQuery, Result<IReadOnlyList<MenuTreeDto>>>
{
    public async ValueTask<Result<IReadOnlyList<MenuTreeDto>>> Handle(
        GetMenuTreeQuery query, CancellationToken cancellationToken)
    {
        var items = await repository.ListAsync(new AllMenuItemsOrderedSpec(), cancellationToken);
        var tree = MenuTree.Build(items);
        return Result<IReadOnlyList<MenuTreeDto>>.Success(Map(tree));
    }

    private static IReadOnlyList<MenuTreeDto> Map(IReadOnlyList<MenuTreeNode> nodes) =>
        nodes.Select(n => new MenuTreeDto(
            n.Item.PublicId.Value,
            n.Item.Name.Value,
            n.Item.Route,
            n.Item.Icon,
            n.Item.DisplayOrder,
            n.Item.IsEnabled,
            Map(n.Children))).ToList();
}
