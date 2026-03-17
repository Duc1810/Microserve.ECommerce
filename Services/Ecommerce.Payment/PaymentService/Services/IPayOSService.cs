using BuildingBlocks.Results;
using Net.payOS.Types;
using PayOS.Models.Webhooks;

namespace PaymentService.Service;
public interface IPayOSService
{
    Task<string> CreatePaymentLinkAsync(long orderCode, decimal amount, string description, string returnUrl, string cancelUrl);
    Task<Result<bool>> ProcessWebhookAsync(Webhook webhook);
}

