using BuildingBlocks.Commands;
using BuildingBlocks.Repository;
using BuildingBlocks.Results;
using MassTransit;
using Microsoft.Extensions.Options;
using Net.payOS.Types;
using PaymentService.Service;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System.Net;

namespace PaymentService.Services;

public class PayOSService : IPayOSService
{
    private readonly PayOSClient _client;
    private readonly ILogger<PayOSService> _logger;
    private readonly UnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public PayOSService(IOptions<PayOSConfig> payOSConfig, ILogger<PayOSService> logger, UnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        var config = payOSConfig.Value;
        _logger = logger;
        _client = new PayOSClient(config.ClientId, config.ApiKey, config.ChecksumKey);
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<string> CreatePaymentLinkAsync(long orderCode, decimal amount, string description, string returnUrl, string cancelUrl)
    {
        int intAmount = (int)Math.Round(amount);

        long expiredAt = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();

        try
        {
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = intAmount,
                Description = description,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                ExpiredAt = expiredAt,
            };

            var response = await _client.PaymentRequests.CreateAsync(paymentRequest);

            _logger.LogInformation("Tạo link thanh toán thành công cho đơn {OrderCode}", orderCode);

            return response.CheckoutUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo PayOS Link cho Order: {OrderCode}", orderCode);
            throw;
        }
    }

    public async Task<Result<bool>> ProcessWebhookAsync(Webhook webhook)
    {
        // Khai báo biến bên ngoài try để catch có thể dùng log
        WebhookData? webhookData = null;
        try
        {
            // 1. Verify webhook
            webhookData = await _client.Webhooks.VerifyAsync(webhook);

            _logger.LogInformation("[PaymentService] Processing webhook for OrderCode: {OrderCode}", webhookData.OrderCode);

            // 2. Parse OrderId (Phải dùng PascalCase: .Description)
            if (!Guid.TryParse(webhookData.Description, out Guid orderId))
            {
                return Result<bool>.ResponseError("INVALID_ID", "Description is not a valid Guid", HttpStatusCode.BadRequest);
            }

            var transactionRepo = _unitOfWork.GetRepository<Models.Transaction>();

            // 3. Check trùng (Phải dùng PascalCase: .Reference)
            var transaction = await transactionRepo.GetByPropertyAsync(t => t.Reference == webhookData.Reference);

            if (transaction != null)
            {
                _logger.LogWarning("[PaymentService] Reference {Reference} already processed.", webhookData.Reference);
                return Result<bool>.ResponseSuccess(true, "Transaction already handled.");
            }

            // 4. Mapping (Tất cả dùng PascalCase)
            var newTransaction = new Models.Transaction
            {
                OrderId = orderId,
                OrderCode = webhookData.OrderCode,
                Amount = webhookData.Amount,
                Reference = webhookData.Reference,
                Description = webhookData.Description,
                AccountNumber = webhookData.AccountNumber,
                CounterAccountName = webhookData.CounterAccountName,
                CounterAccountNumber = webhookData.CounterAccountNumber,
                CounterAccountBankName = webhookData.CounterAccountBankName,
                CreatedAt = DateTime.UtcNow
            };

            await transactionRepo.AddAsync(newTransaction);
            await _unitOfWork.SaveAsync();

            // 5. Notify Saga
            await _publishEndpoint.Publish<IPaymentCompletedEvent>(new
            {
                OrderId = orderId,
                TransactionId = newTransaction.Id
            });

            return Result<bool>.ResponseSuccess(true, "Success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PaymentService] Error processing Webhook");
            return Result<bool>.ResponseError("WEBHOOK_ERROR", ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}

public class PayOSConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
}