

namespace Cart.Application.Dtos;
public class CreateCartItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

