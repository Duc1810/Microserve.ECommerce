

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

public interface IPaymentCompletedEvent
{
    Guid OrderId { get; }
    string TransactionId { get; }
    string PaymentUrl { get; }
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

