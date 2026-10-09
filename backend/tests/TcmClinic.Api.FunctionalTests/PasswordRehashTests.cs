using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.UserAggregate;
using static TcmClinic.Api.FunctionalTests.ProfileTestSupport;

namespace TcmClinic.Api.FunctionalTests;

/// <summary>
/// 登入時自動轉換既有密碼雜湊（SPEC-004 FR-007a、ADR-009）：2026-10-08 前的 SHA-256 雜湊於登入成功後
/// 改為 PBKDF2，且不變更密碼修改時間；已是現行格式者不重寫。以真實 Cookie scheme 登入。
/// </summary>
public class PasswordRehashTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // 情境：以舊 SHA-256 雜湊建立的帳號登入成功後，儲存的雜湊改為 pbkdf2 格式、密碼修改時間不變，
    // 之後仍可用同一密碼登入（使用者無感轉換）。
    // @SPEC-004 @AC-31
    [Fact(DisplayName = "舊 SHA-256 雜湊於登入成功後轉為 PBKDF2 且密碼修改時間不變（FR-007a）")]
    public async Task Legacy_hash_is_upgraded_on_login()
    {
        var changedAt = DateTime.Today.AddDays(-10).AddHours(9);
        await CreateUserAsync(factory, "rehash01");
        await WithDbAsync(factory, async ctx =>
        {
            var user = await ctx.Users.SingleAsync(u => u.Account == UserAccount.From("rehash01"), Ct);
            var (hash, salt) = LegacySha256(Password);
            user.SetPassword(hash, salt, changedAt);
            await ctx.SaveChangesAsync(Ct);
        });

        var login = await LoginRawAsync("rehash01");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var stored = await StoredPasswordAsync("rehash01");
        Assert.StartsWith("pbkdf2-sha512$", stored.Hash, StringComparison.Ordinal);
        Assert.Equal(changedAt, stored.ChangedAt);
        Assert.Equal(HttpStatusCode.OK, (await LoginRawAsync("rehash01")).StatusCode);
    }

    // 情境：已是現行格式的帳號登入時不重寫雜湊（避免每次登入都寫資料庫與產生異動紀錄）。
    // @SPEC-004 @AC-31
    [Fact(DisplayName = "現行格式的雜湊登入時不重寫（FR-007a）")]
    public async Task Current_hash_is_not_rewritten_on_login()
    {
        await CreateUserAsync(factory, "rehash02");
        var before = await StoredPasswordAsync("rehash02");

        var login = await LoginRawAsync("rehash02");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(before, await StoredPasswordAsync("rehash02"));
    }

    private async Task<HttpResponseMessage> LoginRawAsync(string account) =>
        await factory.CreateClient().PostAsJsonAsync("/auth/login", new { account, password = Password }, Ct);

    private async Task<(string Hash, string Salt, DateTime? ChangedAt)> StoredPasswordAsync(string account) =>
        await QueryDbAsync(factory, ctx => ctx.Users.AsNoTracking()
            .Where(u => u.Account == UserAccount.From(account))
            .Select(u => new ValueTuple<string, string, DateTime?>(u.PasswordHash, u.PasswordSalt, u.PasswordChangedAt))
            .SingleAsync(Ct));

    /// <summary>以 2026-10-08 前的演算法產生雜湊：Base64(SHA256(salt ⧺ UTF8(password)))。</summary>
    private static (string Hash, string Salt) LegacySha256(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = SHA256.HashData([.. salt, .. Encoding.UTF8.GetBytes(password)]);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }
}
