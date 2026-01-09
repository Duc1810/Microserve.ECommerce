using Microsoft.AspNetCore.Mvc.RazorPages;

public class NotificationsIndexModel : PageModel
{
    private readonly NotificationApiClient _api;

    public NotificationsIndexModel(NotificationApiClient api) => _api = api;

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Total { get; set; }
    public List<NotificationViewModel> Items { get; set; } = new();

    public async Task OnGet(int pageNumber = 1, int pageSize = 10)
    {
        var (p, s, total, items) = await _api.GetMyNotificationsAsync(pageNumber, pageSize);
        PageIndex = p; PageSize = s; Total = total; Items = items;
    }

    public async Task OnPostMarkRead(int id)
    {
        await _api.MarkAsReadAsync(id);
        await OnGet();
    }

    public async Task OnPostMarkAll()
    {
        await _api.MarkAllAsReadAsync();
        await OnGet();
    }
}
