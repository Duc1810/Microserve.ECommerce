using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Commons.Options;
public class ConsulOptions
{
    public const string SectionName = "Consul";
    public string Address { get; set; } = default!;
}

