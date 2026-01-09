using Email.API.Dtos;

namespace Email.API.Services
{
    public interface IEmailService
    {
        Task SendEmail(EmailRequestDTO request);
        Task SendOrderCreatedEmailAsync(string toEmail, string fullName, Guid orderId, decimal total, int items);

        Task SendVerifyEmailAsync(string toEmail, string token, string? href, string? title = null, string? message = null);
    }
}
