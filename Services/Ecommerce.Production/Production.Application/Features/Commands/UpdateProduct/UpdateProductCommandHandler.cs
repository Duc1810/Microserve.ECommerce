using System.Net;
using Product.Application.Commons;
using ProductEntity = Production.Domain.Entities.Product;

namespace Production.Application.Features.Commands.UpdateProduct;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Result<UpdateProductResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UpdateProductResult>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var productRepository = _unitOfWork.GetRepository<ProductEntity>();

            var product = await productRepository.GetByPropertyAsync(p => p.Id == command.Id);
            if (product is null)
            {
                _logger.LogWarning("[{Handler}.{Method}] not_found id={Id} name={Name}",
                    nameof(UpdateProductCommandHandler), nameof(Handle), command.Id, command.Name);

                return Result<UpdateProductResult>.ResponseError(StatusCodeErrors.ProductNotFound, Messages.ProductNotFound, HttpStatusCode.NotFound);
            }

            // Apply updates
            product.Name = command.Name;
            product.Category = command.Category;
            product.Description = command.Description;
            product.ImageFile = command.ImageFile;
            product.Price = command.Price;
            product.Quantity = command.Quantity;

            await productRepository.UpdateAsync(product);
            await _unitOfWork.SaveAsync();

            return Result<UpdateProductResult>.ResponseSuccess(new UpdateProductResult(product.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Handler}.{Method}] unexpected_error id={Id} name={Name}",
                nameof(UpdateProductCommandHandler), nameof(Handle), command.Id, command.Name);

            return Result<UpdateProductResult>.ResponseError(ErrorCodes.InternalError, ErrorMessages.InternalServerError, HttpStatusCode.InternalServerError);
        }
    }

}
