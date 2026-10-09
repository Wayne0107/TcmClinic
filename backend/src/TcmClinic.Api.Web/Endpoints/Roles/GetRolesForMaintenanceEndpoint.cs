using FastEndpoints;
using Mediator;
using TcmClinic.Api.UseCases.Roles;

namespace TcmClinic.Api.Web.Endpoints.Roles;

/// <summary>角色權限維護清單查詢參數。</summary>
public sealed class GetRolesForMaintenanceRequest
{
    /// <summary>頁碼，從 1 起算；選填，預設 1，小於 1 時視為 1。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每頁筆數，選填；未指定、0 或負數時為 10，最大 100（超過以 100 計）。實際採用值見回應的 pageSize。</summary>
    public int? PageSize { get; set; }
}

/// <summary>
/// GET /roles/maintenance — 角色權限維護頁清單（SPEC-012，US1，FR-001/002/003/004）。
/// 分頁回傳角色代碼／名稱／說明／種類／是否已指派／並行權杖；受「角色權限維護」選單授權控管。
/// 與既有 GET /roles（角色挑選器）並存、不互相影響。
/// </summary>
public sealed class GetRolesForMaintenanceEndpoint(IMediator mediator)
    : Endpoint<GetRolesForMaintenanceRequest, RoleMaintenanceListResult>
{
    public override void Configure()
    {
        Get("/roles/maintenance");
        Policies("MenuAccess:rolePermission");
        Summary(s =>
        {
            s.Summary = "角色權限維護清單";
            s.Description = "分頁列出角色，含角色種類（System/Custom）與是否已被使用者指派；供角色權限維護頁使用。";
            s.ResponseExamples[200] = new RoleMaintenanceListResult(
                [new RoleMaintenanceItemDto(ExampleIds.Role2, "R003", "經辦", "經辦", "Custom", true, "AAAAAAAAB9E=")],
                1, 10, 8);
            s.Responses[200] = "角色維護分頁清單";
            s.Responses[403] = "無「角色權限維護」選單權限";
        });
        Tags("Roles");
    }

    public override async Task HandleAsync(GetRolesForMaintenanceRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRolesForMaintenanceQuery(req.Page, req.PageSize), ct);
        var value = result.Value;

        // GitHub 風格分頁 Link 標頭（比照 GET /lc-applications；Principle V／API 慣例）。
        var lastPage = Math.Max(1, (int)Math.Ceiling(value.TotalCount / (double)value.PageSize));
        string Url(int p) => $"/roles/maintenance?page={p}&pageSize={value.PageSize}";
        var links = new List<string> { $"<{Url(1)}>; rel=\"first\"" };
        if (value.Page > 1)
        {
            links.Add($"<{Url(value.Page - 1)}>; rel=\"prev\"");
        }
        if (value.Page < lastPage)
        {
            links.Add($"<{Url(value.Page + 1)}>; rel=\"next\"");
        }
        links.Add($"<{Url(lastPage)}>; rel=\"last\"");
        HttpContext.Response.Headers.Append("Link", string.Join(", ", links));

        await Send.OkAsync(value, ct);
    }
}
