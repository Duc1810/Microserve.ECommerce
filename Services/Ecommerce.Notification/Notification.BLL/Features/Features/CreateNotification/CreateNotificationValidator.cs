using FluentValidation;

namespace Notification.BLL.Features.Notifications;

public class CreateNotificationValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationValidator()
    {


        RuleFor(x => x.Request.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Request.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(2000).WithMessage("Message must not exceed 2000 characters.");

        RuleFor(x => x.Request.Href)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Href))
            .WithMessage("Href must be a valid URL.");

        RuleFor(x => x.Request.Type)
            .IsInEnum().WithMessage("Invalid notification type.");

        RuleFor(x => x.Request.Metadata)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Metadata))
            .WithMessage("Metadata must not exceed 1000 characters.");
    }

    private static bool BeAValidUrl(string? href)
        => Uri.TryCreate(href, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
