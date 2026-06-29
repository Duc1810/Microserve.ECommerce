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
    private readonly ITransactionService _transactionService;

    public PayOSService(
        IOptions<PayOSConfig> payOSConfig, 
        ILogger<PayOSService> logger, 
        ITransactionService transactionService)
    {
        var config = payOSConfig.Value;
        _logger = logger;
        _client = new PayOSClient(config.ClientId, config.ApiKey, config.ChecksumKey);
        _transactionService = transactionService;
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

            _logger.LogInformation("Successfully created payment link for OrderCode {OrderCode}", orderCode);

            return response.CheckoutUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment link for OrderCode {OrderCode}", orderCode);
            throw;
        }
    }

    public async Task<Result<bool>> ProcessWebhookAsync(Webhook webhook)
    {
        WebhookData? webhookData = null;
        try
        {
            // 1. Verify webhook
            webhookData = await _client.Webhooks.VerifyAsync(webhook);

            _logger.LogInformation("Processing webhook for OrderCode {OrderCode}", webhookData.OrderCode);

            // 2. Parse OrderId from Description
            if (!Guid.TryParse(webhookData.Description, out Guid orderId))
            {
                _logger.LogWarning("Invalid OrderId in webhook description: {Description}", webhookData.Description);
                return Result<bool>.ResponseError("INVALID_ORDER_ID", "Description is not a valid Guid", HttpStatusCode.BadRequest);
            }

            // 3. Create account details
            var accountDetails = new AccountDetails(
                webhookData.AccountNumber,
                webhookData.CounterAccountName,
                webhookData.CounterAccountNumber,
                webhookData.CounterAccountBankName
            );

            // 4. Process payment using TransactionService with idempotent key
            var result = await _transactionService.ProcessPaymentAsync(
                orderId,
                webhookData.OrderCode,
                webhookData.Amount,
                webhookData.Reference,
                webhookData.Description,
                accountDetails
            );

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully processed webhook for OrderId {OrderId}, TransactionId {TransactionId}",
                    orderId, result.Data?.Id);
                return Result<bool>.ResponseSuccess(true, "Webhook processed successfully");
            }
            else
            {
                _logger.LogWarning("Failed to process webhook for OrderId {OrderId}: {Error}",
                    orderId, result.Message);
                return Result<bool>.ResponseError(result.Code, result.Message, result.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for OrderCode {OrderCode}",
                webhookData?.OrderCode ?? "Unknown");
            return Result<bool>.ResponseError("WEBHOOK_ERROR", ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
}

public class PayOSConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
}