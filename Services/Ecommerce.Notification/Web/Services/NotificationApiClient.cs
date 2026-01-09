using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Web.Services;

public sealed class NotificationApiClient
{
    private readonly HttpClient _http;
    private readonly ITokenAccessor _tokenAccessor;

    public NotificationApiClient(HttpClient http, ITokenAccessor tokenAccessor)
    {
        _http = http;
        _tokenAccessor = tokenAccessor;
    }

    private void EnsureAuth()
    {
        var token = _tokenAccessor.GetAccessToken();
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<(int page, int size, int total, List<NotificationViewModel> items)> GetMyNotificationsAsync(int page = 1, int size = 10, CancellationToken ct = default)
    {
        EnsureAuth();
        var res = await _http.GetAsync($"/api/v1/Notification?pageNumber={page}&pageSize={size}", ct);
        res.EnsureSuccessStatusCode();

        using var s = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);

        // Parse theo ApiResponse<GetNotificationsResult>
        var data = doc.RootElement.GetProperty("data").GetProperty("lists");
        var pageIndex = data.GetProperty("pageIndex").GetInt32();
        var pageSize = data.GetProperty("pageSize").GetInt32();
        var count = data.GetProperty("count").GetInt32();

        var list = new List<NotificationViewModel>();
        foreach (var el in data.GetProperty("data").EnumerateArray())
        {
            list.Add(new NotificationViewModel(
                Id: el.GetProperty("id").GetInt32(),
                Title: el.GetProperty("title").GetString() ?? "",
                Message: el.GetProperty("message").GetString() ?? "",
                Href: el.GetProperty("href").GetString(),
                Type: el.GetProperty("type").GetInt32(),
                IsRead: el.GetProperty("isRead").GetBoolean(),
                CreatedAt: el.GetProperty("createdAt").GetDateTime()
            ));
        }

        return (pageIndex, pageSize, count, list);
    }

    public async Task MarkAsReadAsync(int id, CancellationToken ct = default)
    {
        EnsureAuth();
        var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/Notification/{id}/read");
        req.Content = new StringContent("", Encoding.UTF8, "application/json");
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task MarkAllAsReadAsync(CancellationToken ct = default)
    {
        EnsureAuth();
        var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/Notification/read-all");
        req.Content = new StringContent("", Encoding.UTF8, "application/json");
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }
}

public record NotificationViewModel(
    int Id,
    string Title,
    string Message,
    string? Href,
    int Type,
    bool IsRead,
    DateTime CreatedAt
);
