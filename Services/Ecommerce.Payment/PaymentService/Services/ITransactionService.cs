using BuildingBlocks.Results;
using PaymentService.Models;

namespace PaymentService.Services;

public interface ITransactionService
{
    /// <summary>
    /// Process a payment transaction with idempotent key to prevent duplicates
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="orderCode">PayOS order code</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="reference">Payment reference from bank/PayOS</param>
    /// <param name="description">Payment description</param>
    /// <param name="accountDetails">Optional account details</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<Transaction>> ProcessPaymentAsync(
        Guid orderId,
        long orderCode,
        decimal amount,
        string reference,
        string description,
        AccountDetails? accountDetails = null);

    /// <summary>
    /// Get transaction by idempotent key
    /// </summary>
    /// <param name="idempotentKey">Idempotent key</param>
    /// <returns>Transaction if found</returns>
    Task<Transaction?> GetTransactionByIdempotentKeyAsync(string idempotentKey);

    /// <summary>
    /// Get transactions by order ID
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <returns>List of transactions for the order</returns>
    Task<IEnumerable<Transaction>> GetTransactionsByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Generate idempotent key from order ID and reference
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="reference">Payment reference</param>
    /// <returns>Idempotent key</returns>
    string GenerateIdempotentKey(Guid orderId, string reference);
}

public record AccountDetails(
    string? AccountNumber,
    string? CounterAccountName,
    string? CounterAccountNumber,
    string? CounterAccountBankName
);