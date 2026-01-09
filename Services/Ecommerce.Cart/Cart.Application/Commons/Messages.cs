namespace Cart.Application.Commons;

public static class Messages
{
    // ===== Success =====
    public const string OrderCreatedSuccessfully = "Order created successfully";
    public const string CreateCartItemSuccessfully = "Create cart item successfully";

    // ===== Errors (generic) =====
    public const string UnauthorizedAccess = "Unauthorized access.";
    public const string NoUsernameInToken = "No username in token.";
    public const string InternalServerError = "An unexpected error occurred. Please try again later.";

    // ===== Errors (cart) =====
    public const string CartNotFoundForUser = "Cart not found for user.";
    public const string CartItemsEmpty = "Cart items are empty.";
    public const string InvalidQuantity = "Quantity must be greater than 0.";

    // ===== Errors (product) =====
    public static string ProductNotFound(Guid id) => $"Product with id: {id} not found.";

    // ===== Errors (order api) =====
    public static string OrderApiFailed(object? errors)
        => $"Order API failed: {errors ?? "No response content"}";

    // ===== Errors (cart) =====
    public const string CartNotFound = "Cart not found.";

    // ===== Errors (cart item) =====
    public const string RemoveCartItemSuccessfully = "Remove cart item successfully";
    public const string CartItemNotFound = "Cart item not found.";

    public const string CartEmpty = "Cart empty";

}
