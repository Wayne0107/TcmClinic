using Ardalis.Result;
using Ardalis.Specification;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Menus;
using NSubstitute;

namespace TcmClinic.Api.UnitTests.MenuAggregate;

public class MenuHandlerTests
{
    // 情境：同一父層下已存在同名選單 → 建立應回傳 Invalid（FR-007 同層名稱唯一）。
    // @SPEC-002 @AC-16
    [Fact(DisplayName = "同層已存在同名選單時建立回傳 Invalid（FR-007）")]
    public async Task Create_rejects_duplicate_name_in_same_level()
    {
        var repository = Substitute.For<IRepository<MenuItem>>();
        repository
            .AnyAsync(Arg.Any<ISpecification<MenuItem>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreateMenuItemHandler(repository);
        var result = await handler.Handle(
            new CreateMenuItemCommand("使用者", null, "/admin/users", null, 0), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    // 情境（設計決策 R8）：API 以 parentId = null 表示頂層（SPEC-064 起；原為 0），用例建立頂層項目。
    // @SPEC-002 @AC-21
    [Fact(DisplayName = "parentId 為 0 時建立頂層選單，ParentId 存為 null（設計決策 R8）")]
    public async Task Create_with_zero_parent_creates_top_level_item()
    {
        var repository = Substitute.For<IRepository<MenuItem>>();
        repository
            .AnyAsync(Arg.Any<ISpecification<MenuItem>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        MenuItem? added = null;
        repository
            .AddAsync(Arg.Do<MenuItem>(m => added = m), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<MenuItem>());

        var handler = new CreateMenuItemHandler(repository);
        var result = await handler.Handle(
            new CreateMenuItemCommand("首頁", null, "/home", null, 0), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Null(added.ParentId);
    }

    // 情境（FR-020）：指定的父選單不存在 → 回 Invalid 並指出 ParentId，不寫入（避免外鍵違反變成 500）。
    // @SPEC-002 @AC-18
    [Fact(DisplayName = "父選單不存在時建立回傳 Invalid 並指出 ParentId（FR-020）")]
    public async Task Create_rejects_missing_parent()
    {
        var repository = Substitute.For<IRepository<MenuItem>>();
        repository
            .AnyAsync(Arg.Any<ISpecification<MenuItem>>(), Arg.Any<CancellationToken>())
            .Returns(false); // 同名不存在，父選單也不存在

        var handler = new CreateMenuItemHandler(repository);
        var result = await handler.Handle(
            new CreateMenuItemCommand("使用者", Guid.CreateVersion7(), "/admin/users", null, 0), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        var error = Assert.Single(result.ValidationErrors);
        Assert.Equal(nameof(CreateMenuItemCommand.ParentId), error.Identifier);
        Assert.Equal("父選單不存在。", error.ErrorMessage);
        await repository.DidNotReceive().AddAsync(Arg.Any<MenuItem>(), Arg.Any<CancellationToken>());
    }

    // 情境：刪除仍有子項的父選單 → 應阻擋回傳 Invalid（FR-004，不可連動刪整棵子樹）。
    // @SPEC-002 @AC-19
    [Fact(DisplayName = "刪除仍有子項的父選單被阻擋回傳 Invalid（FR-004）")]
    public async Task Delete_blocks_when_item_has_children()
    {
        var item = new MenuItem(MenuItemName.From("系統管理"), null)
        {
            Id = MenuItemId.From(1),
        };

        var repository = Substitute.For<IRepository<MenuItem>>();
        repository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<MenuItem>>(), Arg.Any<CancellationToken>())
            .Returns(item);
        repository
            .AnyAsync(Arg.Any<ISpecification<MenuItem>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new DeleteMenuItemHandler(repository);
        var result = await handler.Handle(new DeleteMenuItemCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    // 情境：刪除不存在的選單 id → 應回傳 NotFound。
    // @SPEC-002 @AC-19
    [Fact(DisplayName = "刪除不存在的選單 id 時回傳 NotFound")]
    public async Task Delete_returns_NotFound_when_missing()
    {
        var repository = Substitute.For<IRepository<MenuItem>>();
        repository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<MenuItem>>(), Arg.Any<CancellationToken>())
            .Returns((MenuItem?)null);

        var handler = new DeleteMenuItemHandler(repository);
        var result = await handler.Handle(new DeleteMenuItemCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
