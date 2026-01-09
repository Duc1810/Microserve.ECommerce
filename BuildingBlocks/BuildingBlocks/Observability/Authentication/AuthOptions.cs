

namespace BuildingBlocks.Observability.Authentication
{
    public sealed class AuthOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public bool RequireHttpsMetadata { get; set; } = true;
        public int ClockSkewMinutes { get; set; } = 2;
        public bool EnableBearerHeaderPatch { get; set; } = true;
        public string ScopeClaimRequired { get; set; } 

        public JwksOptions Jwks { get; set; } = new();

        public sealed class JwksOptions
        {
            public string Uri { get; set; } = default!;
            public string[] ValidAlgorithms { get; set; } 
            public bool AllowInsecureDevCertificate { get; set; } = false;
        }
    }
}
