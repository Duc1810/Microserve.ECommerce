using BuildingBlocks.Observability.BaseEntity;

namespace Production.Domain.Entities;

public class Product :  Entity<Guid>
{
    public string Name { get; set; } = default!;
    public List<string> Category { get; set; } = new();
    public string Description { get; set; } = default!;
    public string ImageFile { get; set; } = default!;
    public int Quantity { get; set; }
    public bool IsDeleted { get; set; }
    public decimal Price { get; set; }
}
