
using Notification.BLL.Features.Notifications.Queries;

namespace Notification.BLL.Features.Features.GetNotification
{
    public class GetNotificationsValidator : AbstractValidator<GetNotificationsQuery>
    {
        public GetNotificationsValidator()
        {
            RuleFor(x => x.Params.PageNumber)
                 .NotNull()
                 .GreaterThan(0);

            RuleFor(x => x.Params.PageSize)
                .NotNull()
                .InclusiveBetween(1, 100);

        }
    }
}
