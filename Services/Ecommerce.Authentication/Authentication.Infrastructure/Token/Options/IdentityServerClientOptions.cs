namespace Authentication.Infrastructure.Token.Options;

    public class IdentityServerClientOptions
    {
        public string IssuerUri { get; set; } = default!;
        public string TokenEndpoint { get; set; } = default!;

        public string RevocationEndpoint { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public string ClientSecret { get; set; } = default!;
        public string Scope { get; set; } = default!;
    }

