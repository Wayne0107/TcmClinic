using FastEndpoints;
using Mediator;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.UseCases.Users;
using TcmClinic.Api.Web.Configurations;

namespace TcmClinic.Api.Web.Endpoints.Users;

/// <summary>使用者清單查詢參數。</summary>
public sealed class GetUsersRequest
{
    /// <summary>頁碼，從 1 起算；選填，預設 1，小於 1 時以 1 計。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每頁筆數，選填；未指定、0 或負數時為 10，最大 100（超過以 100 計）。實際採用值見回應的 pageSize。</summary>
    public int? PageSize { get; set; }

    /// <summary>關鍵字；選填，對帳號或使用者名稱做部分比對（不分大小寫，前後空白忽略）；未提供或空白則不篩選。</summary>
    public string? Keyword { get; set; }
}

/// <summary>
/// GET /users — 使用者清單（US2，FR-010）。分頁 + 關鍵字；僅超管；不含密碼。
/// SPEC-010 代表性示範掛載點（FR-016）：User 實體含 @SensitiveData 欄位（PasswordHash/PasswordSalt），
/// 故本讀取端點為「涉及敏感資料的訪問」，於回應前經 <see cref="IAccessLogRecorder"/> 旁路記錄一筆訪問紀錄。
/// </summary>
public sealed class GetUsersEndpoint(IMediator mediator, IAccessLogRecorder accessLogRecorder)
    : Endpoint<GetUsersRequest, UserListResult>
{
    public override void Configure()
    {
        Get("/users");
        // 以「帳號維護」選單（accountMaintenance）授權（SPEC-005 選單導向授權）。
        Policies("MenuAccess:accountMaintenance");
        Summary(s =>
        {
            s.Summary = "使用者清單";
            s.Description = "分頁列出未刪除的使用者，可依帳號／使用者名稱關鍵字搜尋；回應不含密碼。";
            s.ResponseExamples[200] = new UserListResult(
                [new UserListItem(ExampleIds.User, "superadmin", "系統管理員", ["SUPERADMIN"], ["超級管理員"])],
                1, 20, 1);
            s.Responses[200] = "使用者分頁清單";
            s.Responses[403] = "無此頁面之選單權限";
        });
        Tags("Users");
    }

    public override async Task HandleAsync(GetUsersRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUsersQuery(req.Keyword, req.Page, req.PageSize), ct);
        var value = result.Value;

        // SPEC-010（FR-008/016）：讀取 User（含敏感欄位）視為涉及敏感資料的訪問，旁路記錄一筆訪問紀錄。
        // 記錄失敗不影響本查詢回應（FR-014，盡力寫入於記錄服務內部處理）。RecordCount＝本頁回傳筆數。
        await accessLogRecorder.RecordAsync<User>(
            new AccessLogRequest(
                AccessType.Read, nameof(User), PrimaryKey: null, value.Items.Count, "使用者清單查詢"),
            ct);

        // GitHub 風格分頁 Link 標頭（Principle XIII）。
        var lastPage = Math.Max(1, (int)Math.Ceiling(value.TotalCount / (double)value.PageSize));
        var keywordQuery = string.IsNullOrEmpty(req.Keyword) ? string.Empty : $"&keyword={req.Keyword}";
        string Url(int p) => $"/users?page={p}&pageSize={value.PageSize}{keywordQuery}";

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
