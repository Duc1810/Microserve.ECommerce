// Tests/GetProductsHandlerTests.cs
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Production.Application.UserCases.GetProduct; // namespace của handler/query
using Xunit;
using BuildingBlocks.Observability.Exceptions;

public class GetProductsHandlerTests
{
    private static GetProductsHandler CreateHandlerWithSeed(IEnumerable<Product> seed)
    {
        var uow = FakeUowFactory.WithProducts(seed);
        return new GetProductsHandler(uow);
    }

    [Fact]
    public async Task NoFilter_NoSort_ReturnsPagedData()
    {
        // Arrange
        var handler = CreateHandlerWithSeed(ProductSeed.TenItems());
        var q = new GetProductsQuery(PageNumber: 1, PageSize: 5);

        // Act
        var res = await handler.Handle(q, CancellationToken.None);

        // Assert
        res.Lists.PageIndex.Should().Be(1);
        res.Lists.PageSize.Should().Be(5);
        res.Lists.Count.Should().Be(10);            // total
        res.Lists.Data.Should().HaveCount(5);       // page size
    }

    [Fact]
    public async Task NameFilter_FindsItemsContainingSubstring()
    {
        var handler = CreateHandlerWithSeed(ProductSeed.TenItems());
        var q = new GetProductsQuery(NameFilter: "iPhone", PageNumber: 1, PageSize: 10);

        var res = await handler.Handle(q, CancellationToken.None);

        res.Lists.Count.Should().Be(2); // iPhone 13 & iPhone 14
        res.Lists.Data.Should().OnlyContain(p => p.Name.Contains("iPhone"));
    }

    [Fact]
    public async Task CategoryFilter_WorksOnAnyCategoryString()
    {
        var handler = CreateHandlerWithSeed(ProductSeed.TenItems());
        var q = new GetProductsQuery(CategoryFilter: "apple", PageNumber: 1, PageSize: 20);

        var res = await handler.Handle(q, CancellationToken.None);

        res.Lists.Data.Should().OnlyContain(p => p.Category.Any(c => c.Contains("apple")));
    }

    [Fact]
    public async Task NameAndCategoryFilter_AreAndCombined()
    {
        var handler = CreateHandlerWithSeed(ProductSeed.TenItems());
        var q = new GetProductsQuery(NameFilter: "iPhone", CategoryFilter: "apple", PageNumber: 1, PageSize: 20);

        var res = await handler.Handle(q, CancellationToken.None);

        res.Lists.Count.Should().Be(2);
        res.Lists.Data.Should().OnlyContain(p =>
            p.Name.Contains("iPhone") && p.Category.Any(c => c.Contains("apple")));
    }

    [Fact]
    public async Task SortBy_Name_Ascending()
    {
        var handler = CreateHandlerWithSeed(ProductSeed.TenItems());
        var q = new GetProductsQuery(SortBy: "Name", SortAscending: true, PageNumber: 1, PageSize: 10);

        var res = await handler.Handle(q, CancellationToken.None);

        var names = res.Lists.Data.Select(p => p.Name).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task SortBy_Price_Descending()
    {
        var handler = CreateHandlerWithSeed(ProductSeed.TenItems());
        var q = new GetProductsQuery(SortBy: "Price", SortAscending: false, PageNumber: 1, PageSize: 10);

        var res = await handler.Handle(q, CancellationToken.None);

        var prices = res.Lists.Data.Select(p => p.Price).ToList();
        prices.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Paging_Page2_ReturnsSecondSlice()
    {
        var handler = CreateHandlerWithSeed(ProductSeed.TenItems());
        var q = new GetProductsQuery(PageNumber: 2, PageSize: 3, SortBy: "Id", SortAscending: true);

        var res = await handler.Handle(q, CancellationToken.None);

        res.Lists.Data.Select(x => x.Id).Should().Equal(4, 5, 6);
        res.Lists.Count.Should().Be(10);
    }

    [Fact]
    public async Task EmptyResult_ThrowsNotFoundException()
    {
        var handler = CreateHandlerWithSeed(ProductSeed.TenItems());
        var q = new GetProductsQuery(NameFilter: "zzz-not-exist", PageNumber: 1, PageSize: 10);

        var act = async () => await handler.Handle(q, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
