
using BuildingBlocks.Commands;

namespace BuildingBlocks.Commands;
public interface IStockReservedEvent
{
    Guid OrderId { get; }
}

public interface IStockReservationFailedEvent
{
    Guid OrderId { get; }
    string Reason { get; }
}

public interface IOrderCompletedEvent
{
    Guid OrderId { get; }
    decimal TotalAmount { get; }
    List<OrderCompletedItemDto> Items { get; }
    DateTime CompletedAt { get; }
}

public interface IPaymentCompletedEvent
{
    Guid OrderId { get; }
    string TransactionId { get; }
    string PaymentUrl { get; }
    decimal Amount { get; }
    List<OrderItemDto> Items { get; }
}

public interface IPaymentLinkCreatedEvent
{
    Guid OrderId { get; }
    string PaymentUrl { get; }
    long OrderCode { get; }
}


public interface IPaymentFailedEvent
{
    Guid OrderId { get; }
    string Reason { get; }
}

// DTO for Order service events (matches Order.Application.Dtos.OrderItemDto)
public record OrderCompletedItemDto(
    Guid ProductId,
    int Quantity,
    decimal Price
);

