using Authentication.Infrastructure.Token.Options;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.Extensions.Options;


namespace Authentication.Infrastructure.Data.Configurations
{
    public class ClientStore(
    IOptions<IdentityServerClientOptions> authOptions
) : IClientStore
    {
        public Task<Client> FindClientByIdAsync(string clientId)
        {
            var authValue = authOptions.Value;

            if (clientId != authValue.ClientId)
            {
                return Task.FromResult(new Client());
            }

            return Task.FromResult(new Client
            {
                ClientId = authValue.ClientId,
                ClientSecrets = {
                new Secret(authValue.ClientSecret.Sha256())
            },
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AllowedScopes = { "openid", "profile", "email", "roles", "ecommerce.api", "offline_access" },

                AllowOfflineAccess = true,


                RefreshTokenUsage = TokenUsage.OneTimeOnly,          
                RefreshTokenExpiration = TokenExpiration.Sliding,   
                AbsoluteRefreshTokenLifetime = 60 * 60 * 24 * 30,  
                SlidingRefreshTokenLifetime = 60 * 60 * 24 * 7,      
                UpdateAccessTokenClaimsOnRefresh = true,            

                AccessTokenLifetime = 60 * 60,
            });
        }
    }
}
