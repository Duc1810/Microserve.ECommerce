using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace BuildingBlocks.Observability.Authentication;

public static class JwtAuthExtensions
{
    private static volatile JsonWebKeySet? _cachedJwks;
    private static readonly object _jwksLock = new();

    public static IServiceCollection AddJwtAuthWithManualJwks(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        services.Configure<AuthOptions>(config.GetSection("Authentication"));
        services.AddHttpClient("jwks", (sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<AuthOptions>>().Value;
            http.Timeout = TimeSpan.FromSeconds(10);
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AuthOptions>>().Value;
            if (opts.Jwks.AllowInsecureDevCertificate)
            {
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
            }
            return new HttpClientHandler();
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
          var sp = services.BuildServiceProvider();
          var opts = sp.GetRequiredService<IOptions<AuthOptions>>().Value;
          var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
          var http = httpFactory.CreateClient("jwks");

          options.RequireHttpsMetadata = opts.RequireHttpsMetadata;
          options.IncludeErrorDetails = true;

          options.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuer = true,
              ValidIssuer = opts.Issuer,
              ValidateAudience = true,
              ValidAudience = opts.Audience,
              ValidateLifetime = true,
              ClockSkew = TimeSpan.FromMinutes(opts.ClockSkewMinutes),
              ValidateIssuerSigningKey = true,
              ValidAlgorithms = opts.Jwks.ValidAlgorithms?.Length > 0
                  ? opts.Jwks.ValidAlgorithms
                  : new[] { SecurityAlgorithms.RsaSha256 }
          };

          options.TokenValidationParameters.IssuerSigningKeyResolver =
              (token, securityToken, kid, validationParameters) =>
              {
                  return TryResolveKeysByKid(http, opts.Jwks.Uri, kid);
              };
          options.Events = new JwtBearerEvents
          {
              OnAuthenticationFailed = ctx => { Console.WriteLine("[JWT] AuthFailed: " + ctx.Exception); return Task.CompletedTask; },
              OnChallenge = ctx => { Console.WriteLine("[JWT] Challenge: " + ctx.ErrorDescription); return Task.CompletedTask; },
              OnTokenValidated = ctx => { Console.WriteLine("[JWT] OK token sub=" + ctx.Principal?.FindFirst("sub")?.Value); return Task.CompletedTask; }
          };


          options.Events = new JwtBearerEvents
          {
              OnMessageReceived = ctx =>
              {
                  if (opts.EnableBearerHeaderPatch)
                  {
                      var authHeader = ctx.Request.Headers["Authorization"].ToString();
                      if (authHeader.StartsWith("Bearer Bearer ", StringComparison.OrdinalIgnoreCase))
                          ctx.Token = authHeader.Substring("Bearer ".Length * 2).Trim();
                  }
                  var accessToken = ctx.Request.Query["access_token"].ToString();
                  var path = ctx.HttpContext.Request.Path;
                  if (string.IsNullOrEmpty(ctx.Token) &&
                      !string.IsNullOrEmpty(accessToken) &&
                      path.StartsWithSegments("/hubs/notifications", StringComparison.OrdinalIgnoreCase))
                  {
                      ctx.Token = accessToken;
                  }
                  return Task.CompletedTask;
              }
          };
      });


        services.AddAuthorization(authz =>
        {
            var opts = config.GetSection("Authentication").Get<AuthOptions>()!;
            authz.AddPolicy("ApiScope", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", opts.ScopeClaimRequired);
            });
        });

        return services;
    }

    private static IEnumerable<SecurityKey> TryResolveKeysByKid(HttpClient http, string jwksUri, string? kid)
    {
        if (_cachedJwks != null)
        {
            var hit = FilterKeys(_cachedJwks, kid);
            if (hit != null) return hit;
        }

        lock (_jwksLock)
        {
            if (_cachedJwks != null)
            {
                var hit2 = FilterKeys(_cachedJwks, kid);
                if (hit2 != null) return hit2;
            }

            var latest = http.GetStringAsync(jwksUri).GetAwaiter().GetResult();
            _cachedJwks = new JsonWebKeySet(latest);

            var hit3 = FilterKeys(_cachedJwks, kid) ?? _cachedJwks.Keys;
            return hit3;
        }
    }

    private static IEnumerable<SecurityKey>? FilterKeys(JsonWebKeySet set, string? kid)
    {
        if (string.IsNullOrEmpty(kid))
            return set.Keys;

        var list = set.Keys.Where(k => string.Equals(k.KeyId, kid, StringComparison.Ordinal)).ToList();
        return list.Count > 0 ? list : null;
    }
}
