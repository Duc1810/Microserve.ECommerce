using Bogus;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using product.Infrastructure.Data;
using Production.Domain.Entities;

namespace Production.Infrastructure.Data;

public class ProductSeeder
{
    private readonly ApplicationDbContext _context;

    public ProductSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        Console.WriteLine("🚀 Start seeding products...");

        if (await _context.Products.AnyAsync())
        {
            Console.WriteLine("Products already exist. Skip seeding.");
            return;
        }

        const int total = 2_000;
        const int batchSize = 1_000;

        var faker = new Faker<Production.Domain.Entities.Product>()
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price()))
            .RuleFor(p => p.Quantity, f => f.Random.Int(0, 1000))
            .RuleFor(p => p.ImageFile, f => f.Image.PicsumUrl())
            .RuleFor(p => p.IsDeleted, false)
            .RuleFor(p => p.Category, f => new List<string>
            {
                f.Commerce.Categories(1)[0]
            });

        var products = new List<Production.Domain.Entities.Product>(batchSize);

        for (int i = 0; i < total; i++)
        {
            products.Add(faker.Generate());

            if (products.Count >= batchSize)
            {
                await _context.BulkInsertAsync(products, new BulkConfig
                {
                    BatchSize = batchSize
                });

                products.Clear();

                Console.WriteLine($"Inserted {i + 1} products...");
            }
        }

        if (products.Count > 0)
        {
            await _context.BulkInsertAsync(products);
        }

        Console.WriteLine("🎉 Finished seeding 1,000,000 products!");
    }
}