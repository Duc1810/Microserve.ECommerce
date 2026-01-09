using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;
using System.Globalization;
using System.Text;

namespace Notification.BLL.Features.Event;
public class NotificationCreatedEventConsumer(ISender _sender)
    : IConsumer<CreatedEvent>
{
    public async Task Consume(ConsumeContext<CreatedEvent> context)
    {
        var message = context.Message;
        var userId = message.UserId.ToString();


        string? titleUnsign = message.Title is null ? null : RemoveDiacritics(message.Title);

        var type = NotificationType.USER;

        var createNotificationCommand = new CreateNotificationCommand(new CreateNotificationRequest(message.Title!, titleUnsign, message.Message, message.Href, type, null));

        await _sender.Send(createNotificationCommand, context.CancellationToken);
    }


    private static string? RemoveDiacritics(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        } 

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(capacity: normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

