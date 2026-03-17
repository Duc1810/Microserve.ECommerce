using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Dtos.Products;
public class ProductSearchResult
{
    public ProductDocument Product { get; set; }
    public double Score { get; set; }
    public double MatchPercent { get; set; }
}

