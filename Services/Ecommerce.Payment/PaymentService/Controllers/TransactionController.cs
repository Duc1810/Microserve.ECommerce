using Microsoft.AspNetCore.Mvc;
using PaymentService.Services;
using PaymentService.Models;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(
        ITransactionService transactionService,
        ILogger<TransactionController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Get transaction by idempotent key
    /// </summary>
    /// <param name="idempotentKey">Idempotent key</param>
    /// <returns>Transaction details</returns>
    [HttpGet("by-idempotent-key/{idempotentKey}")]
    public async Task<IActionResult> GetByIdempotentKey(string idempotentKey)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionByIdempotentKeyAsync(idempotentKey);
            
            if (transaction == null)
            {
                return NotFound(new { message = "Transaction not found" });
            }

            return Ok(new
            {
                id = transaction.Id,
                orderId = transaction.OrderId,
                orderCode = transaction.OrderCode,
                amount = transaction.Amount,
                reference = transaction.Reference,
                status = transaction.Status.ToString(),
                idempotentKey = transaction.IdempotentKey,
                createdAt = transaction.CreatedAt,
                processedAt = transaction.ProcessedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction by idempotent key {IdempotentKey}", idempotentKey);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get transactions by order ID
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <returns>List of transactions for the order</returns>
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId)
    {
        try
        {
            var transactions = await _transactionService.GetTransactionsByOrderIdAsync(orderId);
            
            var result = transactions.Select(t => new
            {
                id = t.Id,
                orderId = t.OrderId,
                orderCode = t.OrderCode,
                amount = t.Amount,
                reference = t.Reference,
                status = t.Status.ToString(),
                idempotentKey = t.IdempotentKey,
                createdAt = t.CreatedAt,
                processedAt = t.ProcessedAt
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate idempotent key for testing purposes
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="reference">Payment reference</param>
    /// <returns>Generated idempotent key</returns>
    [HttpPost("generate-idempotent-key")]
    public IActionResult GenerateIdempotentKey([FromBody] GenerateIdempotentKeyRequest request)
    {
        try
        {
            var idempotentKey = _transactionService.GenerateIdempotentKey(request.OrderId, request.Reference);
            
            return Ok(new { idempotentKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating idempotent key for OrderId {OrderId}, Reference {Reference}", 
                request.OrderId, request.Reference);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public record GenerateIdempotentKeyRequest(Guid OrderId, string Reference);