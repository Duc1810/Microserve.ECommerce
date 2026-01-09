
using System.Linq.Expressions;
using BuildingBlocks.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Product.Application.Commons;
using Production.Application.Features.Queries.GetProductById;
using ProductEntity = Production.Domain.Entities.Product;

namespace Product.UnitTests.Handlers;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IGenericRepository<ProductEntity>> _productRepoMock = new();
    private readonly Mock<ILogger<GetProductByIdQueryHandler>> _loggerMock = new();

    private readonly GetProductByIdQueryHandler _sut;

    public GetProductByIdQueryHandlerTests()
    {
        _uowMock.Setup(u => u.GetRepository<ProductEntity>())
                .Returns(_productRepoMock.Object);

        _sut = new GetProductByIdQueryHandler(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Success_When_Product_Found()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var foundProduct = new ProductEntity
        {
            Id = productId,
            Name = "Mac",
            Category = new() { "Tech" },
            Description = "Mac desc",
            ImageFile = "mac.jpg",
            Price = 1999m
        };


        Expression<Func<ProductEntity, bool>>? capturedPredicate = null;
        bool capturedTracked = false;
        string? capturedIncludes = "init-not-null";

        _productRepoMock
            .Setup(r => r.GetByPropertyAsync(
                It.IsAny<Expression<Func<ProductEntity, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()))
            .Callback((Expression<Func<ProductEntity, bool>> predicate, bool tracked, string? includes) =>
            {
                capturedPredicate = predicate;
                capturedTracked = tracked;
                capturedIncludes = includes;
            })
            .ReturnsAsync(foundProduct);

        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert: Result OK
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Product.Should().NotBeNull();
        result.Value!.Product.Id.Should().Be(productId);

        // Assert
        capturedPredicate.Should().NotBeNull();
        var fn = capturedPredicate!.Compile();
        fn(new ProductEntity { Id = productId }).Should().BeTrue();
        fn(new ProductEntity { Id = Guid.NewGuid() }).Should().BeFalse();

        // Assert
        capturedTracked.Should().BeTrue();     
        capturedIncludes.Should().BeNull();

        _productRepoMock.Verify(r => r.GetByPropertyAsync(
            It.IsAny<Expression<Func<ProductEntity, bool>>>(),
            It.Is<bool>(b => b == true),
            It.Is<string?>(s => s == null)), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Product_Not_Found()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _productRepoMock
            .Setup(r => r.GetByPropertyAsync(
                It.IsAny<Expression<Func<ProductEntity, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()))
            .ReturnsAsync((ProductEntity?)null);

        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error?.Code.Should().Be(StatusCodeErrors.ProductNotFound);
        result.Error?.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        _productRepoMock.Verify(r => r.GetByPropertyAsync(
            It.IsAny<Expression<Func<ProductEntity, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<string?>()), Times.Once);
    }
}
