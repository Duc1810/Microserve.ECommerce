using BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Order.Infrastructure.Service.Clients;
public class CartServiceClient : ICartServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CartServiceClient> _logger;

    public CartServiceClient(IHttpClientFactory httpClientFactory, ILogger<CartServiceClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CartServiceUrl");
        _logger = logger;
    }

    public async Task<Result<CartResponse>> GetCartByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending request to Cart Service for UserId: {UserId}", userId);

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/Cart", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CartResponse>(cancellationToken);
                return Result<CartResponse>.ResponseSuccess(data!);
            }

            _logger.LogWarning("Cart Service error: {StatusCode}", response.StatusCode);

            return Result<CartResponse>.Failure(
                "CART_ERROR",
                $"Service returned {response.StatusCode}",
                response.StatusCode
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cart Service connection failed");

            return Result<CartResponse>.Failure(
                "CART_CONNECT_FAILED",
                "Can not connect to Cart Service",
                HttpStatusCode.ServiceUnavailable
            );
        }
    }
}

