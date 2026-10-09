using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using TcmClinic.Api.UseCases.Auth;
using TcmClinic.Api.Web.Configurations;
using TcmClinic.Api.Web.Extensions;
using TcmClinic.Api.Web.Services;

namespace TcmClinic.Api.Web.Endpoints.Auth;

/// <summary>登入請求。</summary>
/// <param name="Account">登入帳號，必填，最長 50 字；不分大小寫，前後空白會忽略。</param>
/// <param name="Password">密碼，必填。</param>
public sealed record LoginRequest(string Account, string Password);

/// <summary>
/// 登入（與換發）成功回應；refresh token 另以 HttpOnly Cookie 設定（SPEC-004 FR-021，ADR-011）。
/// </summary>
/// <param name="AccessToken">access token（JWT）。前端只存在記憶體，每次請求以 <c>Authorization: Bearer</c> 帶上。</param>
/// <param name="TokenType">固定為 <c>Bearer</c>。</param>
/// <param name="ExpiresIn">access token 效期秒數（預設 900）；到期前後以 POST /auth/refresh 換新。</param>
/// <param name="Account">登入帳號（已正規化為小寫）。</param>
/// <param name="DisplayName">顯示名稱。</param>
/// <param name="Roles">所屬角色代碼清單（例如 SUPERADMIN），至少一個。</param>
public sealed record LoginResponse(
    string AccessToken, string TokenType, int ExpiresIn, string Account, string DisplayName, IReadOnlyList<string> Roles);

/// <summary>POST /auth/login — 帳密登入，回傳 access token 並設定 refresh Cookie（US1，FR-002/003/006/006a/006b、FR-021）。</summary>
public sealed class LoginEndpoint(IMediator mediator, AccessTokenIssuer tokenIssuer)
    : Endpoint<LoginRequest, Results<Ok<LoginResponse>, ProblemHttpResult>>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        // 依來源 IP 限速（FR-006b）：超過上限回 429，不進入驗證。
        Options(x => x.RequireRateLimiting(RateLimitConfigs.LoginPolicy));
        Summary(s =>
        {
            s.Summary = "登入";
            s.Description = "以帳號與密碼登入；成功回傳 access token（JWT，15 分鐘），並以 HttpOnly Cookie 設定 refresh token（8 小時滑動）。"
                + "失敗回覆一般性 401（不區分帳號或密碼），連續 5 次失敗鎖定約 15 分鐘。";
            s.ExampleRequest = new LoginRequest("superadmin", "P@ssw0rd123");
            s.ResponseExamples[200] = new LoginResponse("eyJhbGciOiJIUzI1NiIs...", "Bearer", 900, "superadmin", "系統管理員", ["SUPERADMIN"]);
            s.Responses[200] = "登入成功";
            s.Responses[400] = "請求格式錯誤";
            s.Responses[401] = "帳號或密碼錯誤，或帳號遭鎖定";
            s.Responses[429] = "同一來源 IP 登入請求過於頻繁（預設每分鐘 20 次），依 Retry-After 秒數後再試";
        });
        Tags("Auth");
    }

    public override async Task<Results<Ok<LoginResponse>, ProblemHttpResult>>
        ExecuteAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(req.Account, req.Password), ct);

        // 失敗一律回一般性 401（不區分帳號或密碼；集中對應為 ProblemDetails）。
        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        var value = result.Value;
        var (accessToken, expiresIn) = tokenIssuer.Issue(value.Account, value.UserPublicId, value.Session.SessionId);
        RefreshCookie.Write(HttpContext, value.Session.RefreshToken, value.Session.ExpiresAt);

        return TypedResults.Ok(new LoginResponse(
            accessToken, "Bearer", expiresIn, value.Account, value.DisplayName, value.RoleCodes));
    }
}
