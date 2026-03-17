using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Application.Abstractions
{
    public interface ICartServiceClient
    {
        /// <summary>
        /// Fetches the shopping cart details for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="cancellationToken">Token to cancel the request.</param>
        /// <returns>A Result containing the CartResponse if successful.</returns>
        Task<Result<CartResponse>> GetCartByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
public record CartResponse(string UserName, List<CartItemDto> Items, decimal TotalPrice);

public record CartItemDto(Guid ProductId, string ProductName, string Color, decimal Price, int Quantity);