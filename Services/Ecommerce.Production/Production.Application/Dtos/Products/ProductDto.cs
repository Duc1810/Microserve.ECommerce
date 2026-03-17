namespace Production.Application.Dtos.Products;
public record ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ImageFile { get; set; } = default!;

    public List<string> Category { get; set; } = new();
}

public record ProductSearchItemDto
{
    public ProductDto Product { get; set; } = default!;
    public double Score { get; set; }
    public double MatchPercent { get; set; }
}
public class ProductPopularDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public List<string> Category { get; set; } = new();
}

