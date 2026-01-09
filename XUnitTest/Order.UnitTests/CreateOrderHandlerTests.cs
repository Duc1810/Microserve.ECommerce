using BuildingBlocks.Messaging.Events;
using BuildingBlocks.Observability.ApiResponse;
using BuildingBlocks.Observability.Constants;
using BuildingBlocks.Repository;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Order.Application.Commons;
using Order.Application.Dtos;
using Order.Application.Features.Commands.CreateOrder;
using Order.Domain.ValueObjects;
using System.Linq.Expressions;
using System.Net;
using Xunit;
using CustomerEntity = Order.Domain.Models.Customer;
using OrderEntity = Order.Domain.Models.Order;

namespace Order.UnitTests.Handlers
{
    public class CreateOrderCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGenericRepository<CustomerEntity>> _customerRepoMock = new();
        private readonly Mock<IGenericRepository<OrderEntity>> _orderRepoMock = new();
        private readonly Mock<IPublishEndpoint> _busMock = new();
        private readonly Mock<ILogger<CreateOrderCommandHandler>> _loggerMock = new();

        private readonly CreateOrderCommandHandler _sut;

        public CreateOrderCommandHandlerTests()
        {
            _uowMock.Setup(u => u.GetRepository<CustomerEntity>())
                    .Returns(_customerRepoMock.Object);
            _uowMock.Setup(u => u.GetRepository<OrderEntity>())
                    .Returns(_orderRepoMock.Object);
            _uowMock.Setup(u => u.SaveAsync())
                    .Returns(Task.CompletedTask);

            _sut = new CreateOrderCommandHandler(_uowMock.Object, _busMock.Object, _loggerMock.Object);
        }

        private static OrderDto MakeOrderDto(Guid? customerId = null)
        {
            var cid = customerId ?? Guid.NewGuid();
            return new OrderDto(
                CustomerId: cid,
                OrderName: "alice-20250101010101",
                ShippingAddress: new AddressDto(
                    UserName: "Alice",
                    EmailAddress: "alice@example.com",
                    AddressLine: "221B",
                    State: "LDN",
                    ZipCode: "NW1"
                ),
                OrderItems: new List<OrderItemDto>
                {
                    new(Guid.NewGuid(), 2, 10m),
                    new(Guid.NewGuid(), 1, 20m)
                }
            );
        }

        [Fact]
        public async Task Handle_Should_Create_Order_When_Customer_Already_Exists()
        {
            // Arrange
            var orderDto = MakeOrderDto();
            var existingCustomer = CustomerEntity.Create(orderDto.CustomerId, "Alice", "alice@example.com");

            _customerRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CustomerEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(existingCustomer);

            OrderEntity? addedOrder = null;
            _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<OrderEntity>()))
                          .Callback<OrderEntity>(o => addedOrder = o)
                          .Returns(Task.CompletedTask);

            CreatedEvent? publishedEvent = null;
            _busMock.Setup(b => b.Publish(It.IsAny<CreatedEvent>(), It.IsAny<CancellationToken>()))
                    .Callback<CreatedEvent, CancellationToken>((evt, _) => publishedEvent = evt)
                    .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.Handle(new CreateOrderCommand(orderDto), default);

            // Assert: result
            result.IsSuccess.Should().BeTrue();
            result.Value!.Id.Should().Be(addedOrder!.Id);
            result.Message.Should().Be(Messages.OrderCreatedSuccessfully);

            // Assert: order mapping
            addedOrder.Should().NotBeNull();
            addedOrder!.CustomerId.Should().Be(orderDto.CustomerId);
            addedOrder.OrderName.Should().Be(orderDto.OrderName);
            addedOrder.ShippingAddress.Should().BeEquivalentTo(Address.Of(
                orderDto.ShippingAddress.UserName,
                orderDto.ShippingAddress.EmailAddress,
                orderDto.ShippingAddress.AddressLine,
                orderDto.ShippingAddress.State,
                orderDto.ShippingAddress.ZipCode));
            addedOrder.BillingAddress.Should().BeEquivalentTo(addedOrder.ShippingAddress);
            addedOrder.TotalPrice.Should().Be(orderDto.OrderItems.Sum(i => i.Price * i.Quantity));

            // Assert: repo calls
            _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<CustomerEntity>()), Times.Never);
            _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<OrderEntity>()), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(), Times.Once); 

            // Assert: published event
            publishedEvent.Should().NotBeNull();
            publishedEvent!.OrderId.Should().Be(addedOrder.Id);
            publishedEvent.UserId.Should().Be(orderDto.CustomerId);
            publishedEvent.Email.Should().Be(orderDto.ShippingAddress.EmailAddress);
            publishedEvent.FullName.Should().Be(orderDto.ShippingAddress.UserName);
            publishedEvent.TotalItem.Should().Be(orderDto.OrderItems.Count);
            publishedEvent.Price.Should().Be(addedOrder.TotalPrice);
            publishedEvent.Items.Should().HaveCount(orderDto.OrderItems.Count);
        }

        [Fact]
        public async Task Handle_Should_Create_Customer_Then_Order_When_Customer_Not_Exists()
        {
            // Arrange
            var orderDto = MakeOrderDto();

            _customerRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CustomerEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync((CustomerEntity?)null);

            CustomerEntity? addedCustomer = null;
            _customerRepoMock.Setup(r => r.AddAsync(It.IsAny<CustomerEntity>()))
                             .Callback<CustomerEntity>(c => addedCustomer = c)
                             .Returns(Task.CompletedTask);

            OrderEntity? addedOrder = null;
            _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<OrderEntity>()))
                          .Callback<OrderEntity>(o => addedOrder = o)
                          .Returns(Task.CompletedTask);

            _busMock.Setup(b => b.Publish(It.IsAny<CreatedEvent>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.Handle(new CreateOrderCommand(orderDto), default);

            // Assert: customer created
            addedCustomer.Should().NotBeNull();
            addedCustomer!.Id.Should().Be(orderDto.CustomerId);
            addedCustomer.Name.Should().Be(orderDto.ShippingAddress.UserName);
            addedCustomer.Email.Should().Be(orderDto.ShippingAddress.EmailAddress);

            // Assert: order created
            addedOrder.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value!.Id.Should().Be(addedOrder!.Id);

            // Assert: save 
            _uowMock.Verify(u => u.SaveAsync(), Times.Exactly(2));

            _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<CustomerEntity>()), Times.Once);
            _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<OrderEntity>()), Times.Once);
            _busMock.Verify(b => b.Publish(It.IsAny<CreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_InternalError_When_Publish_Fails()
        {
            // Arrange
            var orderDto = MakeOrderDto();
            var existingCustomer = CustomerEntity.Create(orderDto.CustomerId, "Alice", "alice@example.com");

            _customerRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CustomerEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(existingCustomer);

            _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<OrderEntity>()))
                          .Returns(Task.CompletedTask);

            _busMock.Setup(b => b.Publish(It.IsAny<CreatedEvent>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("bus down"));

            // Act
            var result = await _sut.Handle(new CreateOrderCommand(orderDto), default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(ErrorCodes.InternalError);
            result.Error?.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            result.Error?.Message.Should().Be(ErrorMessages.InternalServerError);


            _uowMock.Verify(u => u.SaveAsync(), Times.Once);
            _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<OrderEntity>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Map_OrderItems_And_TotalPrice_Correctly()
        {
            // Arrange
            var orderDto = MakeOrderDto();
            _customerRepoMock.Setup(r => r.GetByPropertyAsync(
                    It.IsAny<Expression<Func<CustomerEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(CustomerEntity.Create(orderDto.CustomerId, "Alice", "alice@example.com"));

            OrderEntity? addedOrder = null;
            _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<OrderEntity>()))
                          .Callback<OrderEntity>(o => addedOrder = o)
                          .Returns(Task.CompletedTask);

            _busMock.Setup(b => b.Publish(It.IsAny<CreatedEvent>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.Handle(new CreateOrderCommand(orderDto), default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            addedOrder.Should().NotBeNull();

            var expectedTotal = orderDto.OrderItems.Sum(i => i.Price * i.Quantity);
            addedOrder!.OrderItems.Should().HaveCount(orderDto.OrderItems.Count);
            addedOrder.TotalPrice.Should().Be(expectedTotal);
        }
    }
}
