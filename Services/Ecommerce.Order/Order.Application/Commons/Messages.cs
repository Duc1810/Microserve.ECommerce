namespace Order.Application.Commons;

public static class Messages
{
    // ===== Success =====
    public const string OrderCreatedSuccessfully = "Order created successfully";
    public const string OrderCreatedTitle = "Order placed successfully";
    public const string OrderFetchedSuccessfully = "order fetched successfully";

    // ===== Templates =====
    private const string OrderCreatedMessageTemplate =
        "Order Id: {0} • Order Name: {1} • Items: {2} • Total: {3}";

    public static string FormatOrderCreatedMessage(Guid orderId, string orderName, int itemCount, decimal total)
        => string.Format(OrderCreatedMessageTemplate, orderId, orderName, itemCount, total.ToString("N0"));

    // ===== Errors =====
    public const string OrderInternalError = "An unexpected error occurred while creating the order.";
    public const string CustomerNotFound = "Customer not found.";
    public const string InvalidOrderItems = "Order items are invalid.";
    public const string OrderNotFound = "Order not found.";
}
