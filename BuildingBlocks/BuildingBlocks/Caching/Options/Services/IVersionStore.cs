using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Services
{
    public interface IVersionStore
    {
        Task<long> GetAsync(string key, CancellationToken ct = default);
        Task<long> BumpAsync(string key, CancellationToken ct = default);
    }
}
