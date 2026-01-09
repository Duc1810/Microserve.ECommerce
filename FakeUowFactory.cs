
using BuildingBlocks.Repository;
using Moq;

public static class FakeUowFactory
{
    public static IUnitOfWork WithProducts(IEnumerable<Product> seed)
    {
        var repo = new FakeProductRepository(seed);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.GetRepository<Product>()).Returns(repo);
        // SaveAsync nếu cần:
        uow.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return uow.Object;
    }
}
