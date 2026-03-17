using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Commons.Options;
public class OllamaOptions
{
    public string BaseUrl { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int TimeoutSeconds { get; set; } = 30;
}

