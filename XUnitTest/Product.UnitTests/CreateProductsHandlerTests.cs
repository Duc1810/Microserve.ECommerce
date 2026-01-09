using BuildingBlocks.Observability.ApiResponse;
using BuildingBlocks.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Product.Application.Commons;
using Production.Application.Dtos.Products;
using Production.Application.Features.Commands.CreateProduct;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ProductEntity = Production.Domain.Entities.Product;

namespace Product.UnitTests.Handlers
{
    public class CreateProductCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGenericRepository<ProductEntity>> _productRepoMock = new();
        private readonly Mock<ILogger<CreateProductCommandHandler>> _loggerMock = new();

        private readonly CreateProductCommandHandler _sut;

        public CreateProductCommandHandlerTests()
        {
            _uowMock.Setup(u => u.GetRepository<ProductEntity>())
                    .Returns(_productRepoMock.Object);

            _sut = new CreateProductCommandHandler(_uowMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Create_Product_And_Save_When_Valid_Request()
        {
            // Arrange
            var newProductPayload = new CreateProductDto("iPhone 16 Pro", new List<string> { "Tech", "Phone" }, "A18, 256GB", "iphone16.jpg", 1299m);
            var command = new CreateProductCommand(newProductPayload);

            ProductEntity? capturedEntity = null;

            _productRepoMock
                .Setup(r => r.AddAsync(It.IsAny<ProductEntity>()))
                .Callback<ProductEntity>(p => capturedEntity = p)
                .Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().NotBeNull();

            capturedEntity.Should().NotBeNull();
            capturedEntity!.Id.Should().NotBeEmpty();
            capturedEntity.Name.Should().Be("iPhone 16 Pro");
            capturedEntity.Category.Should().BeEquivalentTo("Tech", "Phone");
            capturedEntity.Description.Should().Be("A18, 256GB");
            capturedEntity.ImageFile.Should().Be("iphone16.jpg");
            capturedEntity.Price.Should().Be(1299m);

            _productRepoMock.Verify(r => r.AddAsync(It.IsAny<ProductEntity>()), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_InternalError_When_AddAsync_Throws()
        {
            // Arrange
            var newProductPayload = new CreateProductDto("MacBook", new List<string> { "Tech", "Laptop" }, "M4, 16GB","macbook.jpg",1999m);
            var command = new CreateProductCommand(newProductPayload);

            _productRepoMock
                .Setup(r => r.AddAsync(It.IsAny<ProductEntity>()))
                .ThrowsAsync(new InvalidOperationException("db down"));

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error?.Code.Should().Be(ErrorCodes.InternalError);
            _uowMock.Verify(u => u.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_InternalError_When_SaveAsync_Throws()
        {
            // Arrange
            var newProductPayload = new CreateProductDto("iPad", new List<string> { "Tech", "Tablet" }, "M3, 128GB", "ipad.jpg", 899m);
            var command = new CreateProductCommand(newProductPayload);

            _productRepoMock
                .Setup(r => r.AddAsync(It.IsAny<ProductEntity>()))
                .Returns(Task.CompletedTask);

            _uowMock
                .Setup(u => u.SaveAsync())
                .ThrowsAsync(new Exception("unexpected"));

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error?.Code.Should().Be(ErrorCodes.InternalError);
        }
    }
}
