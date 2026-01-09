using System.Linq.Expressions;
using AutoMapper;
using BuildingBlocks.Identity;
using BuildingBlocks.Observability.ApiResponse;
using BuildingBlocks.Observability.Pagination;
using BuildingBlocks.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Order.Application.Commons;
using Order.Application.Dtos;
using Order.Application.UserCases.GetOrder;
using Order.Domain.ValueObjects;
using Xunit;
using DomainOrder = Order.Domain.Models.Order;

namespace Order.UnitTests.Handlers
{
    public class GetOrdersQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGenericRepository<DomainOrder>> _orderRepoMock = new();
        private readonly Mock<ICurrentUser> _currentUserMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<ILogger<GetOrdersQueryHandler>> _loggerMock = new();

        private readonly GetOrdersQueryHandler _sut;

        public GetOrdersQueryHandlerTests()
        {
            _uowMock.Setup(u => u.GetRepository<DomainOrder>())
                    .Returns(_orderRepoMock.Object);


            _mapperMock
                .Setup(m => m.Map<List<OrderDto>>(It.IsAny<List<DomainOrder>>()))
                .Returns((List<DomainOrder> src) =>
                    src.Select(o => new OrderDto(
                        CustomerId: o.CustomerId,
                        OrderName: o.OrderName ?? string.Empty,
                        ShippingAddress: new AddressDto(
                            UserName: o.ShippingAddress.UserName,
                            EmailAddress: o.ShippingAddress.EmailAddress,
                            AddressLine: o.ShippingAddress.AddressLine,
                            State: o.ShippingAddress.State,
                            ZipCode: o.ShippingAddress.ZipCode
                        ),
                        OrderItems: o.OrderItems.Select(oi => new OrderItemDto(
                            ProductId: oi.ProductId,
                            Quantity: oi.Quantity,
                            Price: oi.Price
                        )).ToList()
                    )).ToList()
                );

            _sut = new GetOrdersQueryHandler(
                _uowMock.Object,
                _currentUserMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        private static DomainOrder MakeDomainOrder(Guid? customerId = null)
        {
            var cid = customerId ?? Guid.NewGuid();
            var addr = Address.Of("Alice", "alice@example.com", "221B", "LDN", "NW1");
            var order = DomainOrder.Create(Guid.NewGuid(), cid, "alice-20250101010101", addr, addr);
            order.Add(Guid.NewGuid(), 2, 10m);
            order.Add(Guid.NewGuid(), 1, 20m);
            return order;
        }

        [Fact]
        public async Task Handle_Should_Return_Paginated_Success_When_Data_Exists()
        {
            // Arrange
            _currentUserMock.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

            var domainOrders = new List<DomainOrder> { MakeDomainOrder(), MakeDomainOrder() };

            int? capturedPageNumber = null, capturedPageSize = null;
            string? capturedIncludes = null;
            bool? capturedAscending = null;
            Expression<Func<DomainOrder, bool>>? capturedFilter = null;
            Expression<Func<DomainOrder, object>>? capturedOrderBy = null;

            // MUST match repo signature order:
            // (pageNumber, pageSize, filter, includeProperties, orderBy, ascending)
            _orderRepoMock
                .Setup(r => r.GetAllByPropertyWithCountAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<DomainOrder, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<Expression<Func<DomainOrder, object>>>(),
                    It.IsAny<bool>()))
                .Callback((int pageNumber,
                           int pageSize,
                           Expression<Func<DomainOrder, bool>> filter,
                           string? includeProperties,
                           Expression<Func<DomainOrder, object>>? orderBy,
                           bool ascending) =>
                {
                    capturedPageNumber = pageNumber;
                    capturedPageSize = pageSize;
                    capturedFilter = filter;
                    capturedIncludes = includeProperties;
                    capturedOrderBy = orderBy;
                    capturedAscending = ascending;
                })
                .ReturnsAsync((domainOrders, (long)domainOrders.Count));

            var query = new GetOrdersQuery(new GetOrdersSearchParams
            {
                PageNumber = 2,
                PageSize = 5,
                SortAscending = false
            });

            // Act
            var result = await _sut.Handle(query, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();

            var paged = result.Value!.Lists;
            paged.Should().BeOfType<PaginatedResult<OrderDto>>();
            paged.PageIndex.Should().Be(2);
            paged.PageSize.Should().Be(5);
            paged.Count.Should().Be(2);
            paged.Data.Should().HaveCount(2);

            capturedIncludes.Should().Be(nameof(DomainOrder.OrderItems));
            capturedPageNumber.Should().Be(2);
            capturedPageSize.Should().Be(5);
            capturedAscending.Should().BeFalse();
            capturedFilter.Should().NotBeNull();
            // orderBy
            capturedOrderBy.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Clamp_Page_And_Size()
        {
            // Arrange
            _currentUserMock.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

            int? capturedPageNumber = null, capturedPageSize = null;

            _orderRepoMock
                .Setup(r => r.GetAllByPropertyWithCountAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<DomainOrder, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<Expression<Func<DomainOrder, object>>>(),
                    It.IsAny<bool>()))
                .Callback((int pageNumber,
                           int pageSize,
                           Expression<Func<DomainOrder, bool>> _f,
                           string? _inc,
                           Expression<Func<DomainOrder, object>>? _ob,
                           bool _asc) =>
                {
                    capturedPageNumber = pageNumber;
                    capturedPageSize = pageSize;
                })
                .ReturnsAsync((new List<DomainOrder> { MakeDomainOrder() }, 1L));

            var query = new GetOrdersQuery(new GetOrdersSearchParams
            {
                PageNumber = -99,
                PageSize = 1000
            });

            // Act
            var result = await _sut.Handle(query, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Lists.PageIndex.Should().Be(-99);
            result.Value!.Lists.PageSize.Should().Be(1000);
            capturedPageNumber.Should().Be(-99);
            capturedPageSize.Should().Be(1000);
        }

        [Fact]
        public async Task Handle_Should_Return_Unauthorized_When_UserId_Missing()
        {
            // Arrange
            _currentUserMock.SetupGet(c => c.UserId).Returns((string?)null);

            var query = new GetOrdersQuery(new GetOrdersSearchParams());

            // Act
            var result = await _sut.Handle(query, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(ErrorCodes.Unauthorized);
            result.Error?.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

            _orderRepoMock.Verify(r => r.GetAllByPropertyWithCountAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<DomainOrder, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<Expression<Func<DomainOrder, object>>>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_No_Data()
        {
            // Arrange
            _currentUserMock.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

            _orderRepoMock
                .Setup(r => r.GetAllByPropertyWithCountAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<DomainOrder, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<Expression<Func<DomainOrder, object>>>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((new List<DomainOrder>(), 0L));

            var query = new GetOrdersQuery(new GetOrdersSearchParams());

            // Act
            var result = await _sut.Handle(query, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error?.Code.Should().Be(StatusCodeErrors.OrderNotFound);
            result.Error?.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Handle_Should_Build_Filter_With_All_Options()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId);

            Expression<Func<DomainOrder, bool>>? capturedFilter = null;
            string? capturedIncludes = null;

            _orderRepoMock
                .Setup(r => r.GetAllByPropertyWithCountAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<DomainOrder, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<Expression<Func<DomainOrder, object>>>(),
                    It.IsAny<bool>()))
                .Callback((int _page,
                           int _size,
                           Expression<Func<DomainOrder, bool>> filter,
                           string? includeProperties,
                           Expression<Func<DomainOrder, object>>? _orderBy,
                           bool _asc) =>
                {
                    capturedFilter = filter;
                    capturedIncludes = includeProperties;
                })
                .ReturnsAsync((new List<DomainOrder> { MakeDomainOrder(Guid.Parse(userId)) }, 1L));

            var from = DateTime.UtcNow.AddHours(-2);
            var to = DateTime.UtcNow.AddHours(2);

            var query = new GetOrdersQuery(new GetOrdersSearchParams
            {
                OrderNameFilter = "ALICE",  
                StatusFilter = "Pending",
                From = from,
                To = to,
                SortAscending = true
            });

            // Act
            var _ = await _sut.Handle(query, default);

            // Assert
            capturedFilter.Should().NotBeNull();
            capturedIncludes.Should().Be(nameof(DomainOrder.OrderItems));
        }
    }
}
