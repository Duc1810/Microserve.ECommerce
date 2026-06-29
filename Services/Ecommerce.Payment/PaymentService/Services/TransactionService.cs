using BuildingBlocks.Repository;
using BuildingBlocks.Results;
using MassTransit;
using PaymentService.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Commands;

namespace PaymentService.Services;

public class TransactionService : ITransactionService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        UnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<TransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Result<Transaction>> ProcessPaymentAsync(
        Guid orderId,
        long orderCode,
        decimal amount,
        string reference,
        string description,
        AccountDetails? accountDetails = null)
    {
        try
        {
            // 1. Generate idempotent key
            var idempotentKey = GenerateIdempotentKey(orderId, reference);

            var transactionRepo = _unitOfWork.GetRepository<Transaction>();

            // 2. Check for existing transaction
            var existingTransaction = await transactionRepo.GetByPropertyAsync(t => t.IdempotentKey == idempotentKey);

            if (existingTransaction != null)
            {
                _logger.LogInformation("Transaction with IdempotentKey {IdempotentKey} already exists. Status: {Status}",
                    idempotentKey, existingTransaction.Status);

                return existingTransaction.Status switch
                {
                    TransactionStatus.Completed => Result<Transaction>.ResponseSuccess(existingTransaction, "Transaction already completed"),
                    TransactionStatus.Pending => Result<Transaction>.ResponseError("DUPLICATE_PROCESSING", "Transaction is being processed", HttpStatusCode.Conflict),
                    TransactionStatus.Failed => Result<Transaction>.ResponseError("TRANSACTION_FAILED", "Transaction previously failed", HttpStatusCode.BadRequest),
                    TransactionStatus.Cancelled => Result<Transaction>.ResponseError("TRANSACTION_CANCELLED", "Transaction was cancelled", HttpStatusCode.BadRequest),
                    _ => Result<Transaction>.ResponseError("UNKNOWN_STATUS", "Unknown transaction status", HttpStatusCode.InternalServerError)
                };
            }

            // 3. Create new transaction
            var newTransaction = new Transaction
            {
                OrderId = orderId,
                OrderCode = orderCode,
                Amount = amount,
                Reference = reference,
                Description = description,
                IdempotentKey = idempotentKey,
                Status = TransactionStatus.Pending,
                AccountNumber = accountDetails?.AccountNumber,
                CounterAccountName = accountDetails?.CounterAccountName,
                CounterAccountNumber = accountDetails?.CounterAccountNumber,
                CounterAccountBankName = accountDetails?.CounterAccountBankName,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Save transaction in pending state
            await transactionRepo.AddAsync(newTransaction);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Created new transaction with IdempotentKey {IdempotentKey} for OrderId {OrderId}",
                idempotentKey, orderId);

            try
            {
                // 5. Publish payment completed event
                await _publishEndpoint.Publish<IPaymentCompletedEvent>(new
                {
                    OrderId = orderId,
                    TransactionId = newTransaction.Id.ToString(),
                    PaymentUrl = string.Empty,
                    Amount = amount,
                    Items = new List<Order.Application.Dtos.OrderItemDto>()
                });

                // 6. Update transaction status to completed
                newTransaction.Status = TransactionStatus.Completed;
                newTransaction.ProcessedAt = DateTime.UtcNow;
                newTransaction.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Successfully processed payment for OrderId {OrderId}, Amount {Amount}",
                    orderId, amount);

                return Result<Transaction>.ResponseSuccess(newTransaction, "Payment processed successfully");
            }
            catch (Exception publishEx)
            {
                // 7. Mark transaction as failed if event publishing fails
                newTransaction.Status = TransactionStatus.Failed;
                newTransaction.ProcessedAt = DateTime.UtcNow;
                newTransaction.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveAsync();

                _logger.LogError(publishEx, "Failed to publish payment event for OrderId {OrderId}", orderId);
                return Result<Transaction>.ResponseError("PUBLISH_FAILED", "Failed to publish payment event", HttpStatusCode.InternalServerError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for OrderId {OrderId}", orderId);
            return Result<Transaction>.ResponseError("PROCESSING_ERROR", ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Transaction?> GetTransactionByIdempotentKeyAsync(string idempotentKey)
    {
        var transactionRepo = _unitOfWork.GetRepository<Transaction>();
        return await transactionRepo.GetByPropertyAsync(t => t.IdempotentKey == idempotentKey);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByOrderIdAsync(Guid orderId)
    {
        var transactionRepo = _unitOfWork.GetRepository<Transaction>();
        return await transactionRepo.GetAllAsync(t => t.OrderId == orderId);
    }

    public string GenerateIdempotentKey(Guid orderId, string reference)
    {
        // Create a deterministic idempotent key from OrderId and Reference
        var combined = $"{orderId}:{reference}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hashBytes);
    }
}