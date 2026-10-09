using CsCheck;
using TcmClinic.Api.Web.Endpoints.BuildIdentity;

namespace TcmClinic.Api.UnitTests.Web;

/// <summary>
/// 建置識別的解析規則（SPEC-065 AC-3）：組件版本字串 <c>版本+sha</c> 拆成版本與 sha；
/// 建置時間中繼資料為 ISO 8601 時解析為含時區的時間，沒有或格式錯誤時為 null。
/// </summary>
public class BuildInfoTests
{
    private const string Sha = "f3b3a123a183320c57eed715c303e017ceabd31d";

    // 情境：SDK 寫入的版本字串為「版本+完整 sha」→ 拆成版本 0.1.0 與 sha。
    // @SPEC-065 @AC-3
    [Fact(DisplayName = "版本字串含 + 時拆成版本與 sha（SPEC-065 AC-3）")]
    public void Version_with_plus_splits_into_version_and_sha()
    {
        var info = BuildInfo.Parse($"0.1.0+{Sha}", null);

        Assert.Equal("0.1.0", info.Version);
        Assert.Equal(Sha, info.Sha);
    }

    // 情境：沒有 git 資訊的建置，版本字串沒有 + → sha 為 null，版本照常。
    // @SPEC-065 @AC-3
    [Fact(DisplayName = "版本字串沒有 + 時 sha 為 null（SPEC-065 AC-3）")]
    public void Version_without_plus_has_no_sha()
    {
        var info = BuildInfo.Parse("0.1.0", null);

        Assert.Equal("0.1.0", info.Version);
        Assert.Null(info.Sha);
    }

    // 情境：預先發行版本（含 -）照樣只以 + 分隔，不誤切 - 之後的標記。
    // @SPEC-065 @AC-3
    [Fact(DisplayName = "預先發行版本保留 - 之後的標記（SPEC-065 AC-3）")]
    public void Prerelease_version_is_kept()
    {
        var info = BuildInfo.Parse($"1.2.0-rc.1+{Sha}", null);

        Assert.Equal("1.2.0-rc.1", info.Version);
        Assert.Equal(Sha, info.Sha);
    }

    // 情境：Release 建置寫入的 ISO 8601 建置時間（含時區）解析後保留時區位移。
    // @SPEC-065 @AC-3
    [Fact(DisplayName = "ISO 8601 建置時間解析為含時區的時間（SPEC-065 AC-3）")]
    public void Iso_build_time_is_parsed_with_offset()
    {
        var info = BuildInfo.Parse("0.1.0", "2026-10-08T19:50:00+08:00");

        Assert.Equal(new DateTimeOffset(2026, 10, 8, 19, 50, 0, TimeSpan.FromHours(8)), info.BuildTime);
    }

    // 情境：Debug 建置沒有建置時間、或內容不是時間 → null，不丟例外。
    // @SPEC-065 @AC-3
    [Theory(DisplayName = "沒有或格式錯誤的建置時間為 null（SPEC-065 AC-3）")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-time")]
    public void Missing_or_malformed_build_time_is_null(string? buildTime)
    {
        Assert.Null(BuildInfo.Parse("0.1.0", buildTime).BuildTime);
    }

    // 情境（不變式）：任意版本與 40 碼十六進位 sha 組成的字串，解析後還原為原本的兩段。
    // @SPEC-065 @AC-3
    [Fact(DisplayName = "任意版本與 sha 組合都能還原（SPEC-065 AC-3）")]
    public void Any_version_and_sha_round_trip()
    {
        var gen =
            from major in Gen.Int[0, 99]
            from minor in Gen.Int[0, 99]
            from patch in Gen.Int[0, 99]
            from sha in Gen.Char["0123456789abcdef"].Array[40]
            select (Version: $"{major}.{minor}.{patch}", Sha: new string(sha));

        gen.Sample(x =>
        {
            var info = BuildInfo.Parse($"{x.Version}+{x.Sha}", null);
            return info.Version == x.Version && info.Sha == x.Sha;
        });
    }
}
