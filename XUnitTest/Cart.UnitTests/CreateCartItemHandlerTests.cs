using System.Linq.Expressions;
using System.Net;
using BuildingBlocks.Identity;
using BuildingBlocks.Observability.ApiResponse;
using BuildingBlocks.Repository;
using Cart.Application.Abstractions;
using Cart.Application.Commons;
using Cart.Application.Dtos;
using Cart.Application.Features.Commands.CreateCart;
using Cart.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cart.UnitTests.Handlers
{
    public class CreateCartItemHandlerTests
    {
        // Mocks
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IGenericRepository<ShoppingCart>> _shoppingCartRepoMock = new();
        private readonly Mock<IGenericRepository<CartItem>> _cartItemRepoMock = new();
        private readonly Mock<ICurrentUser> _currentUserMock = new();
        private readonly Mock<IProductService> _productServiceMock = new();
        private readonly Mock<ILogger<CreateCartItemCommandHandler>> _loggerMock = new();

        // SUT
        private readonly CreateCartItemCommandHandler _sut;

        public CreateCartItemHandlerTests()
        {
            _unitOfWorkMock.Setup(u => u.GetRepository<ShoppingCart>()).Returns(_shoppingCartRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<CartItem>()).Returns(_cartItemRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

            _sut = new CreateCartItemCommandHandler(
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _productServiceMock.Object,
                _loggerMock.Object
            );
        }

        private static ProductDto MakeProduct(Guid? id = null, string name = "iPhone", decimal price = 999m) =>
            new ProductDto(
                id ?? Guid.NewGuid(),
                name,
                "Description",
                price,
                10,
                new List<string> { "Category" },
                "image.png"
            );

        [Fact]
        public async Task Handle_Should_Return_Unauthorized_When_UserName_Missing()
        {
            // Arrange
            _currentUserMock.SetupGet(c => c.UserName).Returns((string?)null);

            var request = new CreateCartItemRequest
            {
                ProductId = Guid.NewGuid(),
                Quantity = 1
            };
            var command = new CreateCartItemCommand(request);

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(ErrorCodes.Unauthorized);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Product_Not_Found()
        {
            // Arrange
            _currentUserMock.SetupGet(c => c.UserName).Returns("alice");

            var missingProductId = Guid.NewGuid();
            _productServiceMock
                .Setup(s => s.GetProductAsync(missingProductId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductDto?)null);

            var command = new CreateCartItemCommand(new CreateCartItemRequest
            {
                ProductId = missingProductId,
                Quantity = 1
            });

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(StatusCodeErrors.CartNotFound);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Create_New_Cart_And_Add_Item_When_Cart_Not_Exists()
        {
            // Arrange
            const int addQty = 2;
            const decimal unitPrice = 999m;

            var userName = "alice";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var product = MakeProduct(price: unitPrice);
            _productServiceMock
                .Setup(s => s.GetProductAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            _shoppingCartRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync((ShoppingCart?)null);

            ShoppingCart? capturedNewCart = null;
            _shoppingCartRepoMock
                .Setup(r => r.AddAsync(It.IsAny<ShoppingCart>()))
                .Callback<ShoppingCart>(c =>
                {
                    c.Id = Guid.NewGuid();
                    capturedNewCart = c;
                })
                .Returns(Task.CompletedTask);

            _cartItemRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync((CartItem?)null);

            CartItem? capturedNewItem = null;
            _cartItemRepoMock
                .Setup(r => r.AddAsync(It.IsAny<CartItem>()))
                .Callback<CartItem>(i => capturedNewItem = i)
                .Returns(Task.CompletedTask);

            var command = new CreateCartItemCommand(new CreateCartItemRequest
            {
                ProductId = product.Id,
                Quantity = addQty
            });

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.UserName.Should().Be(userName);
            result.Value.ProductId.Should().Be(product.Id);

            capturedNewCart.Should().NotBeNull();
            capturedNewItem.Should().NotBeNull();
            capturedNewItem!.Quantity.Should().Be(addQty);
            capturedNewItem.Price.Should().Be(unitPrice);

            _shoppingCartRepoMock.Verify(r => r.AddAsync(It.IsAny<ShoppingCart>()), Times.Once);
            _cartItemRepoMock.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Update_Existing_Item_When_Item_Already_In_Cart()
        {
            // Arrange
            const int addQty = 3;
            const decimal newPrice = 450m;

            var userName = "bob";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var product = MakeProduct(name: "PS5", price: newPrice);
            _productServiceMock
                .Setup(s => s.GetProductAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var existingCart = new ShoppingCart { Id = Guid.NewGuid(), UserName = userName };
            _shoppingCartRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(existingCart);

            var existingItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = existingCart.Id,
                ProductId = product.Id,
                Quantity = 1,
                Price = 500m
            };
            _cartItemRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(existingItem);

            _cartItemRepoMock.Setup(r => r.UpdateAsync(existingItem)).Returns(Task.CompletedTask);

            var command = new CreateCartItemCommand(new CreateCartItemRequest
            {
                ProductId = product.Id,
                Quantity = addQty
            });

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingItem.Quantity.Should().Be(1 + addQty);
            existingItem.Price.Should().Be(newPrice);

            _cartItemRepoMock.Verify(r => r.UpdateAsync(existingItem), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Add_New_Item_When_Item_Not_Found_In_Existing_Cart()
        {
            // Arrange
            const decimal price = 450m;

            var userName = "bob";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var product = MakeProduct(name: "PS5", price: price);
            _productServiceMock
                .Setup(s => s.GetProductAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var existingCart = new ShoppingCart { Id = Guid.NewGuid(), UserName = userName };
            _shoppingCartRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(existingCart);

            _cartItemRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync((CartItem?)null);

            CartItem? capturedAddedItem = null;
            _cartItemRepoMock
                .Setup(r => r.AddAsync(It.IsAny<CartItem>()))
                .Callback<CartItem>(ci => capturedAddedItem = ci)
                .Returns(Task.CompletedTask);

            var command = new CreateCartItemCommand(new CreateCartItemRequest
            {
                ProductId = product.Id,
                Quantity = 1
            });

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeTrue();

            capturedAddedItem.Should().NotBeNull();
            capturedAddedItem!.ProductId.Should().Be(product.Id);
            capturedAddedItem.Price.Should().Be(price);

            _cartItemRepoMock.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
        }
    }
}
