using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Dtos.Products;

public class GetProductsCursorResult
{
    public List<ProductDto> Data { get; set; } = new List<ProductDto>();
    public string? NextCursor { get; set; }
}

