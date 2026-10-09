using Ardalis.Result;
using Mediator;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.Core.UserAggregate.Specifications;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.UseCases.Users;

/// <summary>重設使用者密碼（US4，FR-014）。以新 salt 重新雜湊覆寫，舊密碼即失效。</summary>
public sealed record ResetPasswordCommand(Guid Id, string NewPassword) : ICommand<Result>;

public sealed class ResetPasswordHandler(
    IRepository<User> userRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
    : ICommandHandler<ResetPasswordCommand, Result>
{
    public async ValueTask<Result> Handle(
        ResetPasswordCommand command, CancellationToken cancellationToken)
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

        var (hash, salt) = passwordHasher.Hash(command.NewPassword);
        user.SetPassword(hash, salt, timeProvider.GetLocalNow().DateTime);
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}
