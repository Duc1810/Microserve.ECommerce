using Product.Application.Commons;
using Production.Application.Commons.Cursor;
using Production.Application.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Features.Queries.GetProductCursor;
public class GetProductsCursorHandler
: IQueryHandler<GetProductsCursorQuery, Result<GetProductsCursorResult>>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<GetProductsCursorHandler> _logger;
    private readonly IMapper _mapper;

    public GetProductsCursorHandler(
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<GetProductsCursorHandler> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<GetProductsCursorResult>> Handle(
        GetProductsCursorQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var param = query.Params;

            var decoded = Cursor.Decode(param.Cursor);

            var items = await _productRepository.GetByCursorAsync(
                decoded?.CreatedAt,
                decoded?.LastId,
                param.Limit
            );

            if (!items.Any())
            {
                return Result<GetProductsCursorResult>.ResponseError(
                    StatusCodeErrors.ProductNotFound,
                    ErrorMessages.ProductNotFound,
                    HttpStatusCode.NotFound
                );
            }

            var productDtos = _mapper.Map<List<ProductDto>>(items);

            // next cursor
            string? nextCursor = null;

            if (items.Count == param.Limit)
            {
                var last = items.Last();
                DateTime cursorTime = last.CreatedAt ?? DateTime.MinValue;
                nextCursor = Cursor.Encode(cursorTime, last.Id);
            }

            return Result<GetProductsCursorResult>.ResponseSuccess(
                new GetProductsCursorResult
                {
                    Data = productDtos,
                    NextCursor = nextCursor
                }
            );
        }
        catch (Exception ex) {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<GetProductsCursorResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }
}

