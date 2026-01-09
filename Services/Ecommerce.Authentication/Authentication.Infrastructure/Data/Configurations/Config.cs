using Duende.IdentityServer.Models;

namespace Ecommerce.IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),                   
                new IdentityResource(
                    name: "roles",
                    userClaims: new[] { "role" })                 
                {
                    DisplayName = "User roles"
                }
            };


        public static IEnumerable<ApiScope> ApiScopes =>
           new ApiScope[]
            {
                new ApiScope(
                    name: "ecommerce.api",
                    displayName: "Ecommerce API",
                    userClaims: new[] { "sub", "name", "email", "role" } 
                )
            };


        public static IEnumerable<ApiResource> ApiResources =>
           new ApiResource[]
           {
                new ApiResource("ecommerce.api", "Ecommerce API")
                {
                    Scopes = { "ecommerce.api" },
                    UserClaims = { "sub", "name", "email", "role" }      
                }
           };



    }
}
