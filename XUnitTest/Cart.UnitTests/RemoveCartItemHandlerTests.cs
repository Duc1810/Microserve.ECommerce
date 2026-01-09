using BuildingBlocks.Identity;
using BuildingBlocks.Observability.ApiResponse;
using BuildingBlocks.Observability.Constants;
using BuildingBlocks.Repository;
using Cart.Application.Commons;
using Cart.Application.Features.Cart.Commands.RemoveCartItem;
using Cart.Application.Features.Commands.RemoveCartItem;
using Cart.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Net;
using Xunit;

namespace Cart.UnitTests.Handlers
{
    public class RemoveCartItemHandlerTests
    {
        // Mocks
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IGenericRepository<CartItem>> _cartItemRepoMock = new();
        private readonly Mock<ICurrentUser> _currentUserMock = new();
        private readonly Mock<ILogger<RemoveCartItemHandler>> _loggerMock = new();

        // SUT
        private readonly RemoveCartItemHandler _sut;

        public RemoveCartItemHandlerTests()
        {
            // UnitOfWork trả về repo CartItem
            _unitOfWorkMock.Setup(u => u.GetRepository<CartItem>()).Returns(_cartItemRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

            _sut = new RemoveCartItemHandler(
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Unauthorized_When_UserName_Missing()
        {
            // Arrange
            _currentUserMock.SetupGet(c => c.UserName).Returns((string?)null);
            var productId = Guid.NewGuid();
            var command = new RemoveCartItemCommand(productId);

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(ErrorCodes.Unauthorized);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            _cartItemRepoMock.Verify(r => r.GetByPropertyAsync(
                It.IsAny<Expression<Func<CartItem, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()), Times.Never);

            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_CartItem_Not_Found_For_User()
        {
            // Arrange
            const string userName = "alice";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var productId = Guid.NewGuid();
            _cartItemRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync((CartItem?)null);

            var command = new RemoveCartItemCommand(productId);

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(StatusCodeErrors.CartItemNotFound);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _cartItemRepoMock.Verify(r => r.DeleteAsyncById(It.IsAny<Guid>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Delete_Item_And_Save_When_Found()
        {
            // Arrange
            const string userName = "bob";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var productId = Guid.NewGuid();
            var existingItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = Guid.NewGuid(),
                ProductId = productId,
                Cart = new ShoppingCart { Id = Guid.NewGuid(), UserName = userName }
            };

            _cartItemRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(existingItem);

            _cartItemRepoMock
                .Setup(r => r.DeleteAsyncById(existingItem.Id))
                .Returns(Task.CompletedTask);

            var command = new RemoveCartItemCommand(productId);

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.UserName.Should().Be(userName);
            result.Value.ProductId.Should().Be(productId);
            result.Value.IsRemoved.Should().BeTrue();

            _cartItemRepoMock.Verify(r => r.DeleteAsyncById(existingItem.Id), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Use_Correct_Predicate_And_Tracked_Params()
        {
            // Arrange
            const string userName = "charlie";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var productId = Guid.NewGuid();
            var existingItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = Guid.NewGuid(),
                ProductId = productId,
                Cart = new ShoppingCart { Id = Guid.NewGuid(), UserName = userName }
            };

            Expression<Func<CartItem, bool>>? capturedPredicate = null;
            bool capturedTracked = true;           // default value to ensure it’s set
            string? capturedIncludes = "dummy";    // default value to ensure it’s set

            _cartItemRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .Callback((Expression<Func<CartItem, bool>> p, bool t, string? i) =>
                {
                    capturedPredicate = p;
                    capturedTracked = t;
                    capturedIncludes = i;
                })
                .ReturnsAsync(existingItem);

            _cartItemRepoMock
                .Setup(r => r.DeleteAsyncById(existingItem.Id))
                .Returns(Task.CompletedTask);

            var command = new RemoveCartItemCommand(productId);

            // Act
            var _ = await _sut.Handle(command, default);

            // Assert
            capturedPredicate.Should().NotBeNull();
            var matches = capturedPredicate!.Compile();

            // Item cùng user & productId → true
            matches(new CartItem
            {
                ProductId = productId,
                Cart = new ShoppingCart { UserName = userName }
            }).Should().BeTrue();

            // Sai user → false
            matches(new CartItem
            {
                ProductId = productId,
                Cart = new ShoppingCart { UserName = "other" }
            }).Should().BeFalse();

            // Sai product → false
            matches(new CartItem
            {
                ProductId = Guid.NewGuid(),
                Cart = new ShoppingCart { UserName = userName }
            }).Should().BeFalse();

            // Handler dùng tracked:false, includes:null
            capturedTracked.Should().BeFalse();
            capturedIncludes.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Return_InternalError_When_Save_Throws()
        {
            // Arrange
            const string userName = "dave";
            _currentUserMock.SetupGet(c => c.UserName).Returns(userName);

            var productId = Guid.NewGuid();
            var existingItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = Guid.NewGuid(),
                ProductId = productId,
                Cart = new ShoppingCart { Id = Guid.NewGuid(), UserName = userName }
            };

            _cartItemRepoMock
                .Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(existingItem);

            _cartItemRepoMock
                .Setup(r => r.DeleteAsyncById(existingItem.Id))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(u => u.SaveAsync())
                .ThrowsAsync(new InvalidOperationException("db down"));

            var command = new RemoveCartItemCommand(productId);

            // Act
            var result = await _sut.Handle(command, default);

            // Assert (không throw, trả InternalError)
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(ErrorCodes.InternalError);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            result.Error?.Message.Should().Be(ErrorMessages.InternalServerError);
        }
    }
}
