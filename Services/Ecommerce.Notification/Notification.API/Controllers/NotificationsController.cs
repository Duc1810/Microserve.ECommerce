using BuildingBlocks.Identity;
using BuildingBlocks.Observability.ApiResponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.BLL.Commons.Dtos;
using Notification.BLL.Features.Features.MaskRead;
using Notification.BLL.Features.Notifications;
using Notification.BLL.Features.Notifications.Queries;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class NotificationController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = "ApiScope")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
    {
        var result = await _sender.Send(new CreateNotificationCommand(request));
        return result.ToCreatedAtActionResult(nameof(GetNotifications));
    }


    [HttpGet]

    [Authorize(Policy = "ApiScope")]
    public async Task<IActionResult> GetNotifications([FromQuery] PaginationParam request)
    {
        var result = await _sender.Send(new GetNotificationsQuery(request));
        return result.ToActionResult();
    }

    [HttpPatch("{id:int}/read")]
    [Authorize(Policy = "ApiScope")]
    public async Task<IActionResult> MarkAsRead([FromRoute] int id)
    {
        var result = await _sender.Send(new MarkAsReadCommand(id));
        return result.ToActionResult();
    }


    [HttpPatch("read-all")]
    [Authorize(Policy = "ApiScope")]
    public async Task<IActionResult> MarkAllAsRead()
    {

        var result = await _sender.Send(new MarkAllAsReadCommand());
        return result.ToActionResult();
    }
}
