using System.Globalization;
using System.Reflection;

namespace TcmClinic.Api.Web.Endpoints.BuildIdentity;

/// <summary>建置識別：目前執行中建置的版本號、commit sha 與建置時間（SPEC-065），皆取自建置時寫入組件的資訊。</summary>
/// <param name="Version">SemVer 版本號（根目錄 Directory.Build.props 的 Version）。</param>
/// <param name="Sha">建置時 HEAD 的完整 commit sha（40 碼小寫十六進位）；沒有 git 資訊的建置為 null。</param>
/// <param name="BuildTime">建置時間（ISO 8601，含時區）；只有 Release 建置寫入，Debug 建置為 null。</param>
public sealed record BuildInfo(string Version, string? Sha, DateTimeOffset? BuildTime)
{
    /// <summary>建置時間的組件中繼資料鍵（TcmClinic.Api.Web.csproj 於 Release 建置寫入）。</summary>
    public const string BuildTimeKey = "BuildTime";

    /// <summary>本服務的建置識別；組件資訊執行期間不變，只解析一次。</summary>
    public static BuildInfo Current { get; } = FromAssembly(typeof(BuildInfo).Assembly);

    /// <summary>從組件的 <see cref="AssemblyInformationalVersionAttribute"/> 與建置時間中繼資料取得建置識別。</summary>
    public static BuildInfo FromAssembly(Assembly assembly) => Parse(
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == BuildTimeKey)?.Value);

    /// <summary>
    /// 解析組件資訊（SPEC-065 AC-3）。SDK 的 Source Link 把 commit sha 附在版本字串的 <c>+</c> 之後（如 <c>0.1.0+f3b3a12…</c>），
    /// 沒有 <c>+</c> 時 sha 為 null；建置時間不是合法的時間格式時為 null。
    /// </summary>
    public static BuildInfo Parse(string? informationalVersion, string? buildTime)
    {
        var parts = (informationalVersion ?? string.Empty).Split('+', 2);
        var sha = parts.Length == 2 && parts[1].Length > 0 ? parts[1] : null;
        DateTimeOffset? time = DateTimeOffset.TryParse(buildTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
        return new BuildInfo(parts[0], sha, time);
    }
}
