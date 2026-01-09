

using System.ComponentModel.DataAnnotations;

namespace Cart.Infrastructure.Grpc.Clients;

public sealed class ProductGrpcOptions
{
    public const string SectionName = "GrpcSettings";

    [Required, Url]
    public string ProductUrl { get; init; } = default!;
}
