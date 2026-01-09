using AutoMapper;
using BuildingBlocks.Observability.Pagination;
using BuildingBlocks.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Product.Application.Commons;
using Production.Application.Dtos.Products;
using Production.Application.Features.Queries.GetProduct;
using System.Linq.Expressions;
using ProductEntity = Production.Domain.Entities.Product;

namespace Production.UnitTests.Handlers
{
    public class GetProductsQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGenericRepository<ProductEntity>> _productRepoMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<ILogger<GetProductsQueryHandler>> _loggerMock = new();

        private readonly GetProductsQueryHandler _sut;

        public GetProductsQueryHandlerTests()
        {
            _uowMock.Setup(u => u.GetRepository<ProductEntity>())
                    .Returns(_productRepoMock.Object);

            // Map domain
            _mapperMock.Setup(m => m.Map<List<ProductDto>>(It.IsAny<object>()))
                       .Returns((object src) =>
                       {
                           var list = (IEnumerable<ProductEntity>)src;
                           return list.Select(p => new ProductDto
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Price = p.Price,
                               Quantity = p.Quantity,
                               Category = p.Category?.ToList() ?? new List<string>(),
                               ImageFile = p.ImageFile ?? string.Empty
                           }).ToList();
                       });

            _sut = new GetProductsQueryHandler(
                _uowMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        private static ProductEntity MakeProduct(string name, decimal price, int quantity = 10, bool isDeleted = false)
        {
            return new ProductEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "desc",
                Price = price,
                Quantity = quantity,
                Category = new List<string> { "Cat" },
                ImageFile = "img.png",
                IsDeleted = isDeleted,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task Handle_Should_Return_Paginated_Success_And_Verify_Repo_Parameters()
        {
            // Arrange
            var products = new List<ProductEntity>
            {
                MakeProduct("Pro Max", 1000m),
                MakeProduct("Pro Mini", 500m)
            };

            // Capture all args to assert
            int? capturedPageNumber = null, capturedPageSize = null;
            Expression<Func<ProductEntity, bool>>? capturedFilter = null;
            string? capturedIncludes = "dummy";
            Expression<Func<ProductEntity, object>>? capturedOrderBy = null;
            bool? capturedAscending = null;

            _productRepoMock.Setup(r => r.GetAllByPropertyWithCountAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<ProductEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<Expression<Func<ProductEntity, object>>>(),
                    It.IsAny<bool>()
                ))
                .Callback((int pageNumber,
                           int pageSize,
                           Expression<Func<ProductEntity, bool>> filter,
                           string? includeProperties,
                           Expression<Func<ProductEntity, object>>? orderBy,
                           bool ascending) =>
                {
                    capturedPageNumber = pageNumber;
                    capturedPageSize = pageSize;
                    capturedFilter = filter;
                    capturedIncludes = includeProperties;
                    capturedOrderBy = orderBy;
                    capturedAscending = ascending;
                })
                .ReturnsAsync((products, (long)products.Count));

            var query = new GetProductsQuery(new GetProductsSearchParams
            {
                PageNumber = 2,
                PageSize = 5,
                NameFilter = "Pro",
                SortBy = nameof(ProductEntity.Price),
                Descending = true
            });

            // Act
            var result = await _sut.Handle(query, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();

            var paged = result.Value.Page;
            paged.Should().BeOfType<PaginatedResult<ProductDto>>();
            paged.PageIndex.Should().Be(2);
            paged.PageSize.Should().Be(5);
            paged.Count.Should().Be(2);
            paged.Data.Should().HaveCount(2);

            // Assert 
            capturedPageNumber.Should().Be(2);
            capturedPageSize.Should().Be(5);
            capturedIncludes.Should().BeNull();
            capturedAscending.Should().BeFalse();
            capturedOrderBy.Should().NotBeNull();

            // Assert filter 
            capturedFilter.Should().NotBeNull();
            var fn = capturedFilter!.Compile();

            fn(MakeProduct("Pro X", 10m, quantity: 1, isDeleted: false)).Should().BeTrue();
            fn(MakeProduct("Basic", 10m, quantity: 1, isDeleted: false)).Should().BeFalse();
            fn(MakeProduct("Pro X", 10m, quantity: 0, isDeleted: false)).Should().BeFalse();
            fn(MakeProduct("Pro X", 10m, quantity: 1, isDeleted: true)).Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Empty_Result()
        {
            // Arrange
            Expression<Func<ProductEntity, object>>? capturedOrderBy = null;
            bool? capturedAscending = null;
            _productRepoMock.Setup(r => r.GetAllByPropertyWithCountAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<ProductEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<Expression<Func<ProductEntity, object>>>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((Items: new List<ProductEntity>(), TotalCount: 0L));



            var query = new GetProductsQuery(new GetProductsSearchParams
            {
                PageNumber = 1,
                PageSize = 10
            });

            // Act
            var result = await _sut.Handle(query, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(StatusCodeErrors.ProductNotFound);
            result.Error?.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        }


        [Fact]
        public async Task Handle_Should_Pass_OrderBy_Null_When_SortBy_Unknown_And_Ascending_Default_From_Descending_Flag()
        {
            // Arrange
            var items = new List<ProductEntity> { MakeProduct("A", 1m) };

            Expression<Func<ProductEntity, object>>? capturedOrderBy = null;
            bool? capturedAscending = null;

            _productRepoMock.Setup(r => r.GetAllByPropertyWithCountAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<ProductEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<Expression<Func<ProductEntity, object>>>(),
                    It.IsAny<bool>()))
                .Callback((int _p, int _s,
                           Expression<Func<ProductEntity, bool>> _f,
                           string? _inc,
                           Expression<Func<ProductEntity, object>>? orderBy,
                           bool ascending) =>
                {
                    capturedOrderBy = orderBy;
                    capturedAscending = ascending;
                })
                .ReturnsAsync((items, 1L));

            var query = new GetProductsQuery(new GetProductsSearchParams
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "UnknownField",
                Descending = false
            });

            // Act
            var result = await _sut.Handle(query, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedOrderBy.Should().BeNull();
            capturedAscending.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Set_OrderBy_When_SortBy_CreatedAt()
        {
            // Arrange
            var items = new List<ProductEntity> { MakeProduct("X", 2m) };

            Expression<Func<ProductEntity, object>>? capturedOrderBy = null;

            _productRepoMock.Setup(r => r.GetAllByPropertyWithCountAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<ProductEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<Expression<Func<ProductEntity, object>>>(),
                    It.IsAny<bool>()))
                .Callback((int _pn, int _ps,
                           Expression<Func<ProductEntity, bool>> _f,
                           string? _inc,
                           Expression<Func<ProductEntity, object>>? orderBy,
                           bool _asc) =>
                {
                    capturedOrderBy = orderBy;
                })
                .ReturnsAsync((items, 1L));

            var query = new GetProductsQuery(new GetProductsSearchParams
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = nameof(ProductEntity.CreatedAt),
                Descending = true
            });

            // Act
            var result = await _sut.Handle(query, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedOrderBy.Should().NotBeNull();
        }
    }
}
