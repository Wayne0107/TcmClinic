using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace TcmClinic.Api.Web.Endpoints.BuildIdentity;

/// <summary>GET /version — 目前執行中建置的版本號、commit sha 與建置時間（SPEC-065，FR-003～FR-005）。</summary>
/// <remarks>主機資訊，不經用例層（比照 POST /auth/logout）。</remarks>
public sealed class GetVersionEndpoint : EndpointWithoutRequest<Ok<BuildInfo>>
{
    public override void Configure()
    {
        Get("/version");
        // 刻意匿名（憲章原則 I）：部署後由維運人員或腳本確認線上版本，不依賴業務帳號；
        // 回應只有版本號、sha、建置時間，不含套件版本、環境或主機資訊（FR-004、FR-005）。所有環境皆提供。
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "查詢建置識別";
            s.Description = "回傳目前執行中建置的 SemVer 版本號、commit sha 與建置時間，供發版後確認線上執行的 commit。"
                + "不需登入；取不到的值為 null（沒有 git 資訊時 sha 為 null，Debug 建置 buildTime 為 null）。";
            s.ResponseExamples[200] = new BuildInfo(
                "0.1.0", "f3b3a123a183320c57eed715c303e017ceabd31d", new DateTimeOffset(2026, 10, 8, 19, 50, 0, TimeSpan.FromHours(8)));
            s.Responses[200] = "建置識別";
        });
        Tags("Build");
    }

    public override Task<Ok<BuildInfo>> ExecuteAsync(CancellationToken ct) =>
        Task.FromResult(TypedResults.Ok(BuildInfo.Current));
}
