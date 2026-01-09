using System.Linq.Expressions;
using BuildingBlocks.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Product.Application.Commons;
using Production.Application.Features.Commands.UpdateProduct;
using ProductEntity = Production.Domain.Entities.Product;

namespace Product.UnitTests.Handlers;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IGenericRepository<ProductEntity>> _productRepoMock = new();
    private readonly Mock<ILogger<UpdateProductCommandHandler>> _loggerMock = new();

    private readonly UpdateProductCommandHandler _sut;

    public UpdateProductCommandHandlerTests()
    {
        _uowMock.Setup(u => u.GetRepository<ProductEntity>())
                .Returns(_productRepoMock.Object);

        _sut = new UpdateProductCommandHandler(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Product_Not_Found()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductCommand(
            Id: productId,
            Name: "New N",
            Category: new() { "Tech" },
            Description: "Desc",
            ImageFile: "img.jpg",
            Quantity: 5,
            Price: 10m);

        _productRepoMock
            .Setup(r => r.GetByPropertyAsync(
                It.IsAny<Expression<Func<ProductEntity, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()))
            .ReturnsAsync((ProductEntity?)null);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error?.Code.Should().Be(StatusCodeErrors.ProductNotFound);
        result.Error?.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        _productRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ProductEntity>()), Times.Never);
        _uowMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Update_And_Save_When_Product_Exists()
    {
        // Arrange
        var existing = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = "Old",
            Category = new() { "OldCat" },
            Description = "Old desc",
            ImageFile = "old.png",
            Quantity = 1,
            Price = 1m
        };

        var request = new UpdateProductCommand(
            Id: existing.Id,
            Name: "New Name",
            Category: new() { "Tech", "Phone" },
            Description: "New Desc",
            ImageFile: "new.jpg",
            Quantity: 10,
            Price: 999.5m);

        Expression<Func<ProductEntity, bool>>? capturedFilter = null;
        bool capturedTracked = false;
        string? capturedIncludes = "init-not-null";

        _productRepoMock
            .Setup(r => r.GetByPropertyAsync(
                It.IsAny<Expression<Func<ProductEntity, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()))
            .Callback((Expression<Func<ProductEntity, bool>> f, bool tracked, string? inc) =>
            {
                capturedFilter = f;
                capturedTracked = tracked;
                capturedIncludes = inc;
            })
            .ReturnsAsync(existing);

        _productRepoMock.Setup(r => r.UpdateAsync(existing))
                        .Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.SaveAsync())
                .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert: result OK + Id khớp
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(existing.Id);

        // Assert: dữ liệu đã được cập nhật
        existing.Name.Should().Be("New Name");
        existing.Category.Should().BeEquivalentTo("Tech", "Phone");
        existing.Description.Should().Be("New Desc");
        existing.ImageFile.Should().Be("new.jpg");
        existing.Quantity.Should().Be(10);
        existing.Price.Should().Be(999.5m);

        // Assert: predicate đúng theo Id
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Compile()(new ProductEntity { Id = existing.Id }).Should().BeTrue();
        capturedFilter!.Compile()(new ProductEntity { Id = Guid.NewGuid() }).Should().BeFalse();

        // Assert: tham số mặc định GetByPropertyAsync (tracked=true, include=null)
        capturedTracked.Should().BeTrue();
        capturedIncludes.Should().BeNull();

        _productRepoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        _uowMock.Verify(u => u.SaveAsync(), Times.Once);
    }
}
