using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Auth;
using TcmClinic.Api.Web.Extensions;
using TcmClinic.Api.Web.Services;

namespace TcmClinic.Api.Web.Endpoints.Auth;

/// <summary>
/// POST /auth/refresh — 以 refresh Cookie 換發 access token 並輪替 refresh token（SPEC-004 FR-021a、FR-021b）。
/// access token 到期或頁面重新整理（記憶體中的 token 消失）時呼叫；瀏覽器自動帶 refresh Cookie。
/// </summary>
public sealed class RefreshEndpoint(IMediator mediator, AccessTokenIssuer tokenIssuer)
    : EndpointWithoutRequest<Results<Ok<LoginResponse>, ProblemHttpResult>>
{
    public override void Configure()
    {
        Post("/auth/refresh");
        // 以 refresh Cookie 識別，不需 access token（access token 可能已過期）。
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "換發 access token";
            s.Description = "以 HttpOnly refresh Cookie 換發新的 access token，並輪替 refresh token、把 session 到期時間延長為 8 小時後。"
                + "30 秒內以剛輪替掉的 refresh token 再次換發（多分頁同時換新）只回新的 access token、不換 Cookie；"
                + "超過 30 秒再出示舊值視為外洩，撤銷整個 session。";
            s.ResponseExamples[200] = new LoginResponse("eyJhbGciOiJIUzI1NiIs...", "Bearer", 900, "superadmin", "系統管理員", ["SUPERADMIN"]);
            s.Responses[200] = "換發成功";
            s.Responses[401] = "沒有 refresh Cookie，或 session 已撤銷、逾期、帳號無效；同時清除 refresh Cookie";
        });
        Tags("Auth");
    }

    public override async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> ExecuteAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new RefreshSessionCommand(RefreshCookie.Read(HttpContext.Request)), ct);
        if (!result.IsSuccess)
        {
            RefreshCookie.Clear(HttpContext);
            return result.ToProblem();
        }

        var value = result.Value;
        if (value.NewRefreshToken is not null)
        {
            RefreshCookie.Write(HttpContext, value.NewRefreshToken, value.ExpiresAt);
        }

        var (accessToken, expiresIn) = tokenIssuer.Issue(value.Account, value.UserPublicId, value.SessionId);
        return TypedResults.Ok(new LoginResponse(
            accessToken, "Bearer", expiresIn, value.Account, value.DisplayName, value.RoleCodes));
    }
}
