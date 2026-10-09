using System.Security.Claims;
using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.JsonWebTokens;
using TcmClinic.Api.UseCases.Auth;
using TcmClinic.Api.Web.Services;

namespace TcmClinic.Api.Web.Endpoints.Auth;

/// <summary>
/// POST /auth/logout — 撤銷 session 並清除 refresh Cookie（US1，FR-005）。允許匿名且冪等：access token 過期時也要能登出。
/// </summary>
public sealed class LogoutEndpoint(IMediator mediator) : EndpointWithoutRequest<NoContent>
{
    public override void Configure()
    {
        Post("/auth/logout");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "登出";
            s.Description = "撤銷目前的 session（依 refresh Cookie，帶有效 access token 時也依其 session）並清除 refresh Cookie；"
                + "之後該 session 的 access token 與 refresh token 一律 401。未登入也回 204。";
            s.Responses[204] = "已登出";
        });
        Tags("Auth");
    }

    public override async Task<NoContent> ExecuteAsync(CancellationToken ct)
    {
        Guid? sessionId = Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sid), out var sid) ? sid : null;
        await mediator.Send(new LogoutCommand(RefreshCookie.Read(HttpContext.Request), sessionId), ct);
        RefreshCookie.Clear(HttpContext);
        return TypedResults.NoContent();
    }
}
