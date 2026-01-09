
namespace Cart.Application.Abtractions.Dtos;
public record CheckOutCartRequest
{
    public string AddressLine { get; set; } = default!;
    public string State { get; set; } = default!;
    public string ZipCode { get; set; } = default!;

}

