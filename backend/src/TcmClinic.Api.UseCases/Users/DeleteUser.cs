using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Users;

/// <summary>刪除使用者（軟刪除，US5，FR-017）。005：不設防鎖死。</summary>
public sealed record DeleteUserCommand(Guid Id) : ICommand<Result>;

public sealed class DeleteUserHandler(IRepository<User> userRepository)
    : ICommandHandler<DeleteUserCommand, Result>
{
    public async ValueTask<Result> Handle(
        DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (!UserPublicId.TryFrom(command.Id, out var publicId))
        {
            return Result.NotFound();
        }

        var user = await userRepository.FirstOrDefaultAsync(
            new UserByPublicIdSpec(publicId), cancellationToken);
        if (user is null)
        {
            return Result.NotFound();
        }

        await userRepository.DeleteAsync(user, cancellationToken); // 攔截器轉為軟刪除
        return Result.Success();
    }
}
