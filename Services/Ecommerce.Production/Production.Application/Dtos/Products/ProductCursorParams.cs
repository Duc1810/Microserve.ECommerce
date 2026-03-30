namespace Production.Application.Dtos.Products;
public class ProductCursorParams
{
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
}

