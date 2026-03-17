

namespace Production.Domain.Entities;

public class ProductDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ImageFile { get; set; } = string.Empty;
    public List<string> Category { get; set; } = new();
    public bool InStock => Quantity > 0;
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    public float[]? Vector { get; set; }
    public string SemanticText { get; set; } = string.Empty;

    public static ProductDocument FromProduct(Product product, float[]? vector = null)
    {
        return new ProductDocument
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            Category = product.Category,
            IndexedAt = DateTime.UtcNow,
            Vector = vector,
            SemanticText = $"""
Product: {product.Name}
Category: {string.Join(", ", product.Category)}
Description: {product.Description}

This is an ecommerce product available for online purchase.
It belongs to the {string.Join(", ", product.Category)} category.
"""
        };
    }
}

