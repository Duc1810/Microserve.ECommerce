using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Ecommerce.IdentityServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Infrastructure.Data.Configurations
{
    public class ResourceStore : IResourceStore
    {
        public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            return Task.FromResult(
                Config.ApiResources
                    .Where(apiResource => apiResourceNames.Contains(apiResource.Name))
            );
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            return Task.FromResult(
                Config.ApiResources
                    .Where(apiResource => apiResource.Scopes.Any(scopeNames.Contains))
            );
        }

        public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            return Task.FromResult(
                Config.ApiScopes
                    .Where(apiScope => scopeNames.Contains(apiScope.Name))
            );
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            return Task.FromResult(
                Config.IdentityResources
                    .Where(identityResource => scopeNames.Contains(identityResource.Name))
            );
        }
         
        public Task<Resources> GetAllResourcesAsync()
        {
            return Task.FromResult(
                new Resources(
                    Config.IdentityResources,
                    Config.ApiResources,
                    Config.ApiScopes
                )
            );
        }
    }
}
