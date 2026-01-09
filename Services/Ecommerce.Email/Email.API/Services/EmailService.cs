using Email.API.Dtos;
using Email.API.Options;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;


namespace Email.API.Services;

    public class EmailService : IEmailService
    {
        private readonly EmailSetting _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSetting> options, ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendEmail(EmailRequestDTO request)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.DisplayName, _settings.Email));
            email.To.Add(MailboxAddress.Parse(request.ToEmail));
            email.Subject = request.Subject;

            var builder = new BodyBuilder { HtmlBody = request.Body };
            email.Body = builder.ToMessageBody();

            try
            {
                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_settings.Email, _settings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", request.ToEmail);
            }
        }

        public async Task SendOrderCreatedEmailAsync(string toEmail, string fullName, Guid orderId, decimal total, int items)
        {
            var body = $@"
                <h2>Đặt hàng thành công</h2>
                <p>Xin chào {fullName},</p>
                <p>Bạn đã tạo đơn hàng <b>{orderId}</b></p>
                <ul>
                    <li>Số sản phẩm: {items}</li>
                    <li>Tổng tiền: {total:N0}</li>
                </ul>";

            await SendEmail(new EmailRequestDTO
            {
                ToEmail = toEmail,
                Subject = "✅ Xác nhận đơn hàng",
                Body = body
            });
        }

        public async Task SendVerifyEmailAsync(string toEmail, string token, string? href, string? title = null, string? message = null)
        {
            var subject = string.IsNullOrWhiteSpace(title) ? "🔐 Xác thực tài khoản" : title!;
            //var verifyUrl = BuildVerificationLink(href, token);

            var body = $@"
                <h2>{WebUtility.HtmlEncode(subject)}</h2>
                <p>{WebUtility.HtmlEncode(message ?? "Nhấn nút bên dưới để xác thực tài khoản của bạn.")}</p>
                <p>
                    <a href=""{href}"" 
                       style=""display:inline-block;background:#2563eb;color:#fff;
                              text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:600"">
                        Xác thực email
                    </a>
                </p>
                <p>Nếu nút không hoạt động, hãy sao chép liên kết sau vào trình duyệt:</p>
                <p><a href=""{href}""></a></p>";

            await SendEmail(new EmailRequestDTO
            {
                ToEmail = toEmail,
                Subject = subject,
                Body = body
            });
        }

       
    }

