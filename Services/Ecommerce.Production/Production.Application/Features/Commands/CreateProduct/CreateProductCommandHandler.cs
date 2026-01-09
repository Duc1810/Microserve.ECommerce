using Product.Application.Commons;
using ProductEntity = Production.Domain.Entities.Product;
namespace Production.Application.Features.Commands.CreateProduct;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<CreateProductResult>>
{
    private readonly IUnitOfWork _unitOfRepository;
    private readonly ILogger<CreateProductCommandHandler> _logger;
    public CreateProductCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateProductCommandHandler> logger)
    {
        _unitOfRepository = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CreateProductResult>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {

        try
        {
            var product = new ProductEntity
            {
                Id = Guid.NewGuid(),
                Name = command.Product.Name,
                Category = command.Product.Category,
                Description = command.Product.Description,
                ImageFile = command.Product.ImageFile,
                Price = command.Product.Price
            };

            await _unitOfRepository.GetRepository<ProductEntity>().AddAsync(product);
            await _unitOfRepository.SaveAsync();


            return Result<CreateProductResult>.ResponseSuccess(new CreateProductResult(product.Id), Messages.ProductCreatedSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<CreateProductResult>.ResponseError(ErrorCodes.InternalError, ErrorMessages.InternalServerError, HttpStatusCode.InternalServerError
            );
        }

    }
}

