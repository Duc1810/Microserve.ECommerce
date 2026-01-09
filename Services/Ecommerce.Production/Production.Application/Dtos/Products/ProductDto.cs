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


