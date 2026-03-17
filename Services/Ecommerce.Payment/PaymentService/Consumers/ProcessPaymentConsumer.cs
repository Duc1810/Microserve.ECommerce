using BuildingBlocks.Commands;
using MassTransit;
using PaymentService.Services;

namespace PaymentService.Consumers
{
    public class ProcessPaymentConsumer : IConsumer<IProcessPaymentCommand>
    {
        private readonly ILogger<ProcessPaymentConsumer> _logger;
        private PayOSService _payOSService;

        public ProcessPaymentConsumer(ILogger<ProcessPaymentConsumer> logger, PayOSService payOSService)
        {
            _logger = logger;
            _payOSService = payOSService;
        }
        public async Task Consume(ConsumeContext<IProcessPaymentCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}", context.Message.OrderId, context.Message.Amount);
            try
            {
                // Using a timestamp or a mapping logic to convert Guid to long.
                long orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Execute service to create payment link via PayOS SDK
                var checkoutUrl = await _payOSService.CreatePaymentLinkAsync(
                    orderCode: orderCode,
                    amount: message.Amount,
                    description: $"Payment for order {message.OrderId}",
                    returnUrl: "https://your-app.com/success",
                    cancelUrl: "https://your-app.com/cancel"
                );

                // Successfully created the link. Notify the Saga to proceed.
                _logger.LogInformation("Payment link created successfully for OrderId: {OrderId}", message.OrderId);

                await context.Publish<IPaymentLinkCreatedEvent>(new
                {
                    OrderId = message.OrderId,
                    PaymentUrl = checkoutUrl,
                    OrderCode = orderCode // Keep track of the numeric code used in PayOS
                });

            }
            catch (Exception ex)
            {
                // Critical failure: log the error and notify Saga to trigger compensation (rollback)
                _logger.LogError(ex, "Critical error occurred while creating payment link for OrderId: {OrderId}", message.OrderId);

                await context.Publish<IPaymentFailedEvent>(new
                {
                    OrderId = message.OrderId,
                    ErrorMessage = ex.Message,
                    FailureTimestamp = DateTime.UtcNow
                });
            }
        }
    }
}
