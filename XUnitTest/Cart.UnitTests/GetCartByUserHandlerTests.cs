using BuildingBlocks.Identity;
using BuildingBlocks.Observability.ApiResponse;
using BuildingBlocks.Observability.Constants;
using BuildingBlocks.Repository;
using Cart.Application.Commons;
using Cart.Application.Features.Queries.GetCart;
using Cart.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Net;


namespace Cart.UnitTests.Handlers
{
    public class GetCartByUserHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGenericRepository<ShoppingCart>> _cartRepoMock = new();
        private readonly Mock<ICurrentUser> _currentUserMock = new();
        private readonly Mock<ILogger<GetCartByUserHandler>> _loggerMock = new();
        private readonly GetCartByUserHandler _sut;

        public GetCartByUserHandlerTests()
        {
            _uowMock.Setup(u => u.GetRepository<ShoppingCart>()).Returns(_cartRepoMock.Object);
            _sut = new GetCartByUserHandler(_uowMock.Object, _loggerMock.Object, _currentUserMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Return_Unauthorized_When_UserName_Missing()
        {
            // Arrange
            _currentUserMock.SetupGet(c => c.UserName).Returns((string?)null);

            // Act
            var result = await _sut.Handle(new GetCartByUserQuery(), default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(ErrorCodes.Unauthorized);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            _cartRepoMock.Verify(r => r.GetByPropertyAsync(
                It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Cart_Not_Found()
        {
            // Arrange
            const string userName = "alice";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            _cartRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync((ShoppingCart?)null);

            // Act
            var result = await _sut.Handle(new GetCartByUserQuery(), default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(StatusCodeErrors.CartNotFound);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Handle_Should_Return_Ok_With_Empty_Items_When_Cart_Empty()
        {
            // Arrange
            const string userName = "bob";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var emptyCart = new ShoppingCart
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                CartItems = new List<CartItem>(),
            };

            _cartRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(emptyCart);

            // Act
            var result = await _sut.Handle(new GetCartByUserQuery(), default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.UserName.Should().Be(userName);
            result.Value.Items.Should().NotBeNull().And.BeEmpty();
            result.Value.TotalPrice.Should().Be(0m);
        }

        [Fact]
        public async Task Handle_Should_Return_Ok_With_Items_When_Cart_Has_Items()
        {
            // Arrange
            const string userName = "charlie";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var p1 = Guid.NewGuid();
            var p2 = Guid.NewGuid();

            var cartWithItems = new ShoppingCart
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                CartItems = new List<CartItem>
                {
                    new() { Id = Guid.NewGuid(), CartId = Guid.NewGuid(), ProductId = p1, ProductName = "iPhone", Color = "Red", Price = 500m, Quantity = 1 },
                    new() { Id = Guid.NewGuid(), CartId = Guid.NewGuid(), ProductId = p2, ProductName = "PS5",   Color = "White", Price = 300m, Quantity = 2 }
                },
            };

            _cartRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(cartWithItems);

            // Act
            var result = await _sut.Handle(new GetCartByUserQuery(), default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.UserName.Should().Be(userName);
            result.Value.TotalPrice.Should().Be(1100m);

            result.Value.Items.Should().HaveCount(2);
            result.Value.Items.Should().ContainSingle(i =>
                i.ProductId == p1 && i.ProductName == "iPhone" && i.Color == "Red" && i.Price == 500m && i.Quantity == 1);
            result.Value.Items.Should().ContainSingle(i =>
                i.ProductId == p2 && i.ProductName == "PS5" && i.Color == "White" && i.Price == 300m && i.Quantity == 2);
        }

        [Fact]
        public async Task Handle_Should_Pass_Tracked_True_And_Include_CartItems()
        {
            // Arrange
            const string userName = "daisy";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            Expression<Func<ShoppingCart, bool>>? capturedPredicate = null;
            bool capturedTracked = false;
            string? capturedIncludes = null;

            var anyCart = new ShoppingCart { Id = Guid.NewGuid(), UserName = userName, CartItems = new List<CartItem>()};

            _cartRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .Callback((Expression<Func<ShoppingCart, bool>> p, bool t, string? inc) =>
                {
                    capturedPredicate = p;
                    capturedTracked = t;
                    capturedIncludes = inc;
                })
                .ReturnsAsync(anyCart);

            // Act
            var _ = await _sut.Handle(new GetCartByUserQuery(), default);

            // Assert 
            capturedPredicate.Should().NotBeNull();
            var match = capturedPredicate!.Compile();
            match(new ShoppingCart { UserName = userName }).Should().BeTrue();
            match(new ShoppingCart { UserName = "other" }).Should().BeFalse();


            capturedTracked.Should().BeTrue();
            capturedIncludes.Should().Be(nameof(ShoppingCart.CartItems));
        }

        [Fact]
        public async Task Handle_Should_Return_InternalError_When_Repository_Throws()
        {
            // Arrange
            const string userName = "eve";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            _cartRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ThrowsAsync(new InvalidOperationException("db down"));

            // Act
            var result = await _sut.Handle(new GetCartByUserQuery(), default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(ErrorCodes.InternalError);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            result.Error?.Message.Should().Be(ErrorMessages.InternalServerError);
        }
    }
}
