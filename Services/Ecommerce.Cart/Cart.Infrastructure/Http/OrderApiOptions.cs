

namespace Cart.Infrastructure.Http;


public class OrderApiOptions
{
    public const string SectionName = "OrderApi";

    public string BaseAddress { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
}
    


