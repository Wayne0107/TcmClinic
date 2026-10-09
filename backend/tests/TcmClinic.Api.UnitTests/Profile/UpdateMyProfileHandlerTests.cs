using Ardalis.Result;
using Ardalis.Specification;
using Microsoft.Extensions.Logging.Abstractions;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.SharedKernel;
using TcmClinic.Api.UseCases.Auth;
using TcmClinic.Api.UseCases.Profile;
using NSubstitute;

namespace TcmClinic.Api.UnitTests.Profile;

/// <summary>修改本人個人資料用例（SPEC-013）：非預期錯誤的失敗紀錄與命令字串表示不含密碼。</summary>
public class UpdateMyProfileHandlerTests
{
    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string UserName => "bob";
    }

    // 情境（spec Edge Case：非預期錯誤、FR-016）：儲存時拋出例外 → 記一筆 Failure（不含密碼），
    // 並回傳 CriticalError，錯誤訊息為可直接顯示的「……修改失敗」（不外洩例外細節）。
    // @SPEC-013 @AC-27
    [Fact(DisplayName = "儲存發生非預期錯誤時記失敗紀錄並回傳修改失敗訊息（013，FR-015、FR-016）")]
    public async Task Unexpected_save_error_is_logged_and_returned_as_critical_error()
    {
        var user = new User(UserAccount.From("bob"), UserName.From("Bob"))
            .SetPassword("hash", "salt", DateTime.Now);
        var repository = Substitute.For<IRepository<User>>();
        repository.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(user);
        repository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new InvalidOperationException("資料庫暫時無法使用"));
        var recorder = Substitute.For<IUserLogRecorder>();

        var handler = new UpdateMyProfileHandler(
            new FakeCurrentUser(), repository, Substitute.For<IPasswordHasher>(),
            Substitute.For<ILoginThrottle>(), recorder, TimeProvider.System,
            NullLogger<UpdateMyProfileHandler>.Instance);

        var result = await handler.Handle(
            new UpdateMyProfileCommand("Bob2", null, null, null, null, null), CancellationToken.None);

        Assert.Equal(ResultStatus.CriticalError, result.Status);
        Assert.Equal(new[] { "個人資料維護 (bob) [Bob2] 修改失敗" }, result.Errors);
        Assert.DoesNotContain(result.Errors, e => e.Contains("資料庫暫時無法使用"));
        await recorder.Received(1).RecordAsync(
            Arg.Is<UserLogRequest>(r =>
                r.FunctionId == "PersonalData"
                && r.Action == "Modify"
                && r.Status == UserLogStatus.Failure
                && r.Content.StartsWith("個人資料維護 (bob) [")
                && r.Content.Contains("修改失敗")),
            Arg.Any<CancellationToken>());
    }

    // 情境：命令的字串表示（例如被記錄或除錯輸出）不得包含任何密碼值（SC-006）。
    // @SPEC-013 @AC-23
    [Fact(DisplayName = "命令字串表示不含密碼值（013，SC-006）")]
    public void Command_ToString_hides_passwords()
    {
        var command = new UpdateMyProfileCommand(
            "Bob", "0912", "b@c.co", "CurrentPass123", "NewPassword456", "NewPassword456");

        var text = command.ToString();

        Assert.DoesNotContain("CurrentPass123", text);
        Assert.DoesNotContain("NewPassword456", text);
        Assert.Contains("Bob", text);
    }
}
