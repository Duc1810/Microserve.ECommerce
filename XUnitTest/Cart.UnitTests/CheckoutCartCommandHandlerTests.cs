using BuildingBlocks.Caching.Services;
using BuildingBlocks.Identity;
using BuildingBlocks.Observability.ApiResponse;
using BuildingBlocks.Repository;
using Cart.Application.Abstractions;
using Cart.Application.Abtractions.Dtos;
using Cart.Application.Commons;
using Cart.Application.Features.Cart.Commands.CheckoutCart;
using Cart.Application.Features.Commands.CheckoutCart;
using Cart.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Order.Application.Dtos;
using Order.Application.Features.Commands.CreateOrder;
using System.Linq.Expressions;
using System.Net;


namespace Cart.UnitTests.Handlers
{
    public class CheckoutCartCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGenericRepository<ShoppingCart>> _cartRepoMock = new();
        private readonly Mock<IOrderApi> _orderApiMock = new();
        private readonly Mock<ICurrentUser> _currentUserMock = new();
        private readonly Mock<IRedisService> _redisMock = new();
        private readonly Mock<ILogger<CheckoutCartCommandHandler>> _loggerMock = new();

        private readonly CheckoutCartCommandHandler _sut;

        public CheckoutCartCommandHandlerTests()
        {
            _uowMock.Setup(u => u.GetRepository<ShoppingCart>())
                    .Returns(_cartRepoMock.Object);

            _uowMock.Setup(u => u.SaveAsync())
                    .Returns(Task.CompletedTask);

            _sut = new CheckoutCartCommandHandler(
                _uowMock.Object,
                _orderApiMock.Object,
                _currentUserMock.Object,
                _redisMock.Object,
                _loggerMock.Object
            );
        }

        private static ApiResponseNew<CreateOrderResult> ApiOk(Guid id) =>
            new ApiResponseNew<CreateOrderResult>
            {
                Success = true,
                Data = new CreateOrderResult(id)
            };

        private static ApiResponseNew<CreateOrderResult> ApiFail(string errors = "bad") =>
            new ApiResponseNew<CreateOrderResult>
            {
                Success = false,
                Errors = errors
            };

        [Fact]
        public async Task Handle_Should_Return_Unauthorized_When_UserName_Or_UserId_Missing()
        {

            _currentUserMock.SetupGet(c => c.UserName).Returns((string?)null);
            _currentUserMock.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

            var payload1 = new CheckOutCartRequest { AddressLine = "A", State = "S", ZipCode = "Z" };
            var res1 = await _sut.Handle(new CheckoutCartCommand(payload1), default);

            res1.IsSuccess.Should().BeFalse();
            res1.Error?.Code.Should().Be(ErrorCodes.Unauthorized);
            res1.Error?.StatusCode.Should().Be(HttpStatusCode.Unauthorized);


            _currentUserMock.SetupGet(c => c.UserName).Returns("alice");
            _currentUserMock.SetupGet(c => c.UserId).Returns((string?)null);

            var payload2 = new CheckOutCartRequest { AddressLine = "A", State = "S", ZipCode = "Z" };
            var res2 = await _sut.Handle(new CheckoutCartCommand(payload2), default);

            res2.IsSuccess.Should().BeFalse();
            res2.Error?.Code.Should().Be(ErrorCodes.Unauthorized);
            res2.Error?.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            _cartRepoMock.Verify(r => r.GetByPropertyAsync(
                It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Cart_NotFound()
        {
            _currentUserMock.SetupGet(c => c.UserName).Returns("alice");
            _currentUserMock.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

            _cartRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(), true, It.IsAny<string?>()))
                .ReturnsAsync((ShoppingCart?)null);

            var request = new CheckOutCartRequest { AddressLine = "A", State = "S", ZipCode = "Z" };
            var result = await _sut.Handle(new CheckoutCartCommand(request), default);

            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(StatusCodeErrors.CartNotFound);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Error?.Message.Should().Be(Messages.CartNotFoundForUser);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_CartItems_Empty()
        {
            _currentUserMock.SetupGet(c => c.UserName).Returns("alice");
            _currentUserMock.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

            var emptyCart = new ShoppingCart
            {
                Id = Guid.NewGuid(),
                UserName = "alice",
                CartItems = new List<CartItem>()
            };

            _cartRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(), true, It.IsAny<string?>()))
                .ReturnsAsync(emptyCart);

            var request = new CheckOutCartRequest { AddressLine = "A", State = "S", ZipCode = "Z" };
            var result = await _sut.Handle(new CheckoutCartCommand(request), default);

            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(StatusCodeErrors.CartNotFound);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Error?.Message.Should().Be(Messages.CartItemsEmpty);
        }

        [Fact]
        public async Task Handle_Should_Create_Order_Clear_Cart_And_Cache_On_Success()
        {
            // Arrange current user
            var userId = Guid.NewGuid();
            var userName = "alice";
            var email = "alice@example.com";

            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);
            _currentUserMock.SetupGet(c => c.Email).Returns(email);
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId.ToString());

            // Cart & items
            var cartId = Guid.NewGuid();
            var product1 = Guid.NewGuid();
            var product2 = Guid.NewGuid();

            var cart = new ShoppingCart
            {
                Id = cartId,
                UserName = userName,
                CartItems = new List<CartItem>
                {
                    new() { ProductId = product1, Price = 10m, Quantity = 2 },
                    new() { ProductId = product2, Price = 20m, Quantity = 1 },
                }
            };

            string? capturedIncludes = null;
            _cartRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(), true, It.IsAny<string?>()))
                .Callback((Expression<Func<ShoppingCart, bool>> _, bool tracked, string? inc) =>
                {
                    tracked.Should().BeTrue();
                    capturedIncludes = inc;
                })
                .ReturnsAsync(cart);

            // Order API OK
            var newOrderId = Guid.NewGuid();
            _orderApiMock
                .Setup(a => a.CreateOrderAsync(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()))
                .Callback<OrderDto, CancellationToken>((dto, _) =>
                {
                    // Validate mapped dto
                    dto.CustomerId.Should().Be(userId);
                    dto.OrderName.Should().StartWith(userName + "-");

                    dto.ShippingAddress.UserName.Should().Be(userName);
                    dto.ShippingAddress.EmailAddress.Should().Be(email);
                    dto.ShippingAddress.AddressLine.Should().Be("221B");
                    dto.ShippingAddress.State.Should().Be("LDN");
                    dto.ShippingAddress.ZipCode.Should().Be("NW1");

                    dto.OrderItems.Should().HaveCount(2);
                    dto.OrderItems.Should().ContainSingle(i => i.ProductId == product1 && i.Price == 10m && i.Quantity == 2);
                    dto.OrderItems.Should().ContainSingle(i => i.ProductId == product2 && i.Price == 20m && i.Quantity == 1);
                })
                .ReturnsAsync(ApiOk(newOrderId));

            _cartRepoMock.Setup(r => r.DeleteAsyncById(cartId)).Returns(Task.CompletedTask);
            _redisMock.Setup(r => r.RemoveAsync($"cart:{userName}")).Returns(Task.CompletedTask);

            var request = new CheckOutCartRequest
            {
                AddressLine = "221B",
                State = "LDN",
                ZipCode = "NW1"
            };

            // Act
            var result = await _sut.Handle(new CheckoutCartCommand(request), default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value?.IsSuccess.Should().BeTrue();
            result.Value.OrderId.Should().NotBeNullOrWhiteSpace();
            result.Message.Should().Be(Messages.OrderCreatedSuccessfully);

            capturedIncludes.Should().Be(nameof(ShoppingCart.CartItems));

            _orderApiMock.Verify(a => a.CreateOrderAsync(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()), Times.Once);
            _cartRepoMock.Verify(r => r.DeleteAsyncById(cartId), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(), Times.Once);
            _redisMock.Verify(r => r.RemoveAsync($"cart:{userName}"), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Not_Clear_Cart_When_OrderApi_Fails()
        {
            // Arrange user & cart
            var userId = Guid.NewGuid();
            var userName = "alice";

            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId.ToString());

            var cart = new ShoppingCart
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                CartItems = new List<CartItem>
                {
                    new() { ProductId = Guid.NewGuid(), Price = 5m, Quantity = 1 }
                }
            };

            _cartRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(), true, It.IsAny<string?>()))
                .ReturnsAsync(cart);

            _orderApiMock
                .Setup(a => a.CreateOrderAsync(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiFail("order bad"));

            var request = new CheckOutCartRequest { AddressLine = "A", State = "S", ZipCode = "Z" };

            // Act
            var result = await _sut.Handle(new CheckoutCartCommand(request), default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(StatusCodeErrors.Order_APi_Failed);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Error?.Message.Should().Contain("Order API");

            _cartRepoMock.Verify(r => r.DeleteAsyncById(It.IsAny<Guid>()), Times.Never);
            _uowMock.Verify(u => u.SaveAsync(), Times.Never);
            _redisMock.Verify(r => r.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Allow_Null_Email_And_Send_Empty_Email_In_Shipping()
        {
            var userId = Guid.NewGuid();
            var userName = "bob";

            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);
            _currentUserMock.SetupGet(c => c.Email).Returns((string?)null);
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId.ToString());

            var cart = new ShoppingCart
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                CartItems = new List<CartItem> { new() { ProductId = Guid.NewGuid(), Price = 5m, Quantity = 1 } }
            };

            _cartRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(), true, It.IsAny<string?>()))
                .ReturnsAsync(cart);

            _orderApiMock
                .Setup(a => a.CreateOrderAsync(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()))
                .Callback<OrderDto, CancellationToken>((dto, _) =>
                {
                    dto.ShippingAddress.EmailAddress.Should().Be("");
                })
                .ReturnsAsync(ApiOk(Guid.NewGuid()));

            _cartRepoMock.Setup(r => r.DeleteAsyncById(cart.Id)).Returns(Task.CompletedTask);
            _redisMock.Setup(r => r.RemoveAsync($"cart:{userName}")).Returns(Task.CompletedTask);

            var request = new CheckOutCartRequest { AddressLine = "A", State = "S", ZipCode = "Z" };
            var result = await _sut.Handle(new CheckoutCartCommand(request), default);

            result.IsSuccess.Should().BeTrue();
        }
    }
}
