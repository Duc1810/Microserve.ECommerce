using Microsoft.Extensions.Logging;
using Moq;
using PaymentService.Services;
using PaymentService.Models;
using BuildingBlocks.Repository;
using MassTransit;
using Xunit;

namespace PaymentService.Tests;

public class TransactionServiceTests
{
    private readonly Mock<UnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<ILogger<TransactionService>> _mockLogger;
    private readonly Mock<IGenericRepository<Transaction>> _mockTransactionRepo;
    private readonly TransactionService _transactionService;

    public TransactionServiceTests()
    {
        _mockUnitOfWork = new Mock<UnitOfWork>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockLogger = new Mock<ILogger<TransactionService>>();
        _mockTransactionRepo = new Mock<IGenericRepository<Transaction>>();

        _mockUnitOfWork.Setup(x => x.GetRepository<Transaction>())
            .Returns(_mockTransactionRepo.Object);

        _transactionService = new TransactionService(
            _mockUnitOfWork.Object,
            _mockPublishEndpoint.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void GenerateIdempotentKey_SameInputs_ReturnsSameKey()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var reference = "REF123456";

        // Act
        var key1 = _transactionService.GenerateIdempotentKey(orderId, reference);
        var key2 = _transactionService.GenerateIdempotentKey(orderId, reference);

        // Assert
        Assert.Equal(key1, key2);
        Assert.NotEmpty(key1);
    }

    [Fact]
    public void GenerateIdempotentKey_DifferentInputs_ReturnsDifferentKeys()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var reference = "REF123456";

        // Act
        var key1 = _transactionService.GenerateIdempotentKey(orderId1, reference);
        var key2 = _transactionService.GenerateIdempotentKey(orderId2, reference);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public async Task ProcessPaymentAsync_DuplicateTransaction_ReturnsExistingTransaction()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var reference = "REF123456";
        var existingTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Reference = reference,
            Status = TransactionStatus.Completed,
            IdempotentKey = _transactionService.GenerateIdempotentKey(orderId, reference)
        };

        _mockTransactionRepo.Setup(x => x.GetByPropertyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Transaction, bool>>>()))
            .ReturnsAsync(existingTransaction);

        // Act
        var result = await _transactionService.ProcessPaymentAsync(
            orderId, 12345, 100.00m, reference, "Test payment");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existingTransaction.Id, result.Data.Id);
        Assert.Contains("already completed", result.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_NewTransaction_CreatesSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var reference = "REF123456";

        _mockTransactionRepo.Setup(x => x.GetByPropertyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Transaction, bool>>>()))
            .ReturnsAsync((Transaction?)null);

        _mockTransactionRepo.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveAsync())
            .Returns(Task.CompletedTask);

        _mockPublishEndpoint.Setup(x => x.Publish<BuildingBlocks.Commands.IPaymentCompletedEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _transactionService.ProcessPaymentAsync(
            orderId, 12345, 100.00m, reference, "Test payment");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Completed, result.Data.Status);
        Assert.NotNull(result.Data.ProcessedAt);
    }
}