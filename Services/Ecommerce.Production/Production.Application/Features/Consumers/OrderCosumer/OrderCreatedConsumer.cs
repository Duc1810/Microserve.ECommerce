using BuildingBlocks.Caching.Services;
using BuildingBlocks.Messaging.Events;
using MassTransit;
namespace Production.Application.Features.Consumers.OrderCosumer;

public class OrderCreatedConsumer : IConsumer<CreatedEvent>
{
    private readonly IUnitOfWork _unitOfRepository;
    private readonly IVersionStore _redisVersionService;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    public OrderCreatedConsumer(IUnitOfWork unitOfRepository, IVersionStore redisVersionService, ILogger<OrderCreatedConsumer> logger)
    {
        _unitOfRepository = unitOfRepository;
        _redisVersionService = redisVersionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreatedEvent> context)
    {
        var createdOrderEvent = context.Message;
        var productRepository = _unitOfRepository.GetRepository<Domain.Entities.Product>();

        //get all products in the order
        var listProductEvent = createdOrderEvent.Items.Select(i => i.ProductId).ToList();

        var products = await productRepository.GetAllByPropertyAsync(
            filter: product => listProductEvent.Contains(product.Id),
            asTracking: true
        );

        //map to dictionary for easy access
        var productIds = products.ToDictionary(p => p.Id);

        //check if any product is missing
        var missingProductIds = listProductEvent.Where(id => !productIds.ContainsKey(id)).ToList();
        if (missingProductIds.Count > 0)
        {
            _logger.LogWarning("[{Handler}.{Method}] missing_products ids={Ids}", nameof(OrderCreatedConsumer), nameof(Consume), string.Join(", ", missingProductIds));
            throw new BadRequestException($"Missing products: {string.Join(", ", missingProductIds)}");
        }



        foreach (var item in createdOrderEvent.Items)
        {
            var product = productIds[item.ProductId];
            if (product.Quantity < item.Quantity)
            {
                break;
            }
            product.Quantity -= item.Quantity;
        }

        await _unitOfRepository.SaveAsync();

        await _redisVersionService.BumpAsync("product:list:ver");

        var categories = products
            .SelectMany(p => p.Category ?? Enumerable.Empty<string>())
            .Distinct();
        foreach (var c in categories)
            await _redisVersionService.BumpAsync($"product:list:ver:category:{c}");

        foreach (var id in listProductEvent.Distinct())
            await _redisVersionService.BumpAsync($"product:detail:ver:{id}");
    }
}

