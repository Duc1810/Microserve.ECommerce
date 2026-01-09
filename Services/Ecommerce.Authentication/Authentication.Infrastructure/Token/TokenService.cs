using Authentication.Application.Abstractions;
using Authentication.Infrastructure.Token.Options;
using BuildingBlocks.Observability.Exceptions;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Authentication.Infrastructure.Token;

public class TokenService : ITokenService, ILogoutService
{

    private readonly HttpClient _httpClient;
    private readonly IdentityServerClientOptions _identityServcerClientOptions;
    private readonly ILogger<TokenService> _logger;
    private const string Handler = nameof(TokenService);

    public TokenService(HttpClient httpClient, IOptions<IdentityServerClientOptions> options, ILogger<TokenService> logger)
    {
        _httpClient = httpClient;
        _identityServcerClientOptions = options.Value;
        _logger = logger;
    }


    public async Task<TokenResponse> RequestPasswordTokenAsync(string username, string password, CancellationToken cancellationToken)
    {

        _logger.LogInformation("[{Handler}.{Method}] start user={User}", Handler, nameof(RequestPasswordTokenAsync), username);

        var tokenRequest = new PasswordTokenRequest
        {
            Address = _identityServcerClientOptions.TokenEndpoint,
            ClientId = _identityServcerClientOptions.ClientId,
            ClientSecret = _identityServcerClientOptions.ClientSecret,
            Scope = _identityServcerClientOptions.Scope,
            UserName = username,
            Password = password
        };

        // request the token from the token endpoint
        var tokenResponse = await _httpClient.RequestPasswordTokenAsync(tokenRequest, cancellationToken);


        if (tokenResponse.IsError)
        {
            _logger.LogError("[{Handler}.{Method}] token_error http={Status} err={Err} desc={Desc}",
                Handler, nameof(RequestPasswordTokenAsync), tokenResponse.HttpStatusCode, tokenResponse.Error, tokenResponse.ErrorDescription);
            throw new BadRequestException("Create token failded");
        }

        _logger.LogInformation("[{Handler}.{Method}] success expiresIn={ExpiresIn}s",
                Handler, nameof(RequestPasswordTokenAsync), tokenResponse.ExpiresIn);

        return tokenResponse;
    }


    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {

        _logger.LogInformation("[{Handler}.{Method}] start", Handler, nameof(RefreshTokenAsync));

        var refreshTokenRequest = new RefreshTokenRequest
        {
            Address = _identityServcerClientOptions.TokenEndpoint,
            ClientId = _identityServcerClientOptions.ClientId,
            ClientSecret = _identityServcerClientOptions.ClientSecret,
            RefreshToken = refreshToken
        };

        // use the refresh token to get a new access token
        var tokenResponse = await _httpClient.RequestRefreshTokenAsync(refreshTokenRequest, ct);

        if (tokenResponse.IsError)
        {
            _logger.LogError("Refresh token error: {Code} {Err} - {Desc}", tokenResponse.HttpStatusCode, tokenResponse.Error, tokenResponse.ErrorDescription);
            throw new BadRequestException("RefreshToken faild");
        }

        return tokenResponse;
    }


    public async Task<TokenRevocationResponse> RevokeTokenAsync(string refreshToken, CancellationToken ct)
    {

        _logger.LogInformation("[{Handler}.{Method}] start", Handler, nameof(RevokeTokenAsync));
        var revokeEndpoint = _identityServcerClientOptions.TokenEndpoint.Replace("/token", "/revocation", StringComparison.OrdinalIgnoreCase);

        var req = new TokenRevocationRequest
        {
            Address = revokeEndpoint,
            ClientId = _identityServcerClientOptions.ClientId,
            ClientSecret = _identityServcerClientOptions.ClientSecret,
            Token = refreshToken,
            TokenTypeHint = "refresh_token"
        };

        var revocationResponse = await _httpClient.RevokeTokenAsync(req, ct);
        if (revocationResponse.IsError)
        {
            _logger.LogError("Revoke token error: {Err}", revocationResponse.Error);
        }
           
        return revocationResponse;
    }


    public async Task<bool> LogoutAsync(string? refreshToken, CancellationToken ct)
    {

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var resp = await RevokeTokenAsync(refreshToken, ct);
            if (resp.IsError)
            {
                _logger.LogWarning("Failed to revoke refresh token: {Err} - {Desc}", resp.Error);
                throw new BadRequestException("Login Failed");
            }
        }

        return true;

    }


    public async Task<string> BuildEndSessionUrlAsync(string? idTokenHint, string? postLogoutRedirectUri, CancellationToken cancellationToken)
    {
        var disco = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
        {
            Address = _identityServcerClientOptions.IssuerUri,
            Policy = new DiscoveryPolicy
            {
                RequireHttps = false,
                ValidateIssuerName = false
            }
        }, cancellationToken);

        if (disco.IsError)
            throw new BadRequestException($"Discovery error: {disco.Error}");

        var endSession = disco.EndSessionEndpoint ?? $"{_identityServcerClientOptions.IssuerUri?.TrimEnd('/')}/connect/endsession";

        var uri = new RequestUrl(endSession).Create(new Parameters
        {
            { OidcConstants.EndSessionRequest.IdTokenHint, idTokenHint ?? string.Empty },
            //{ OidcConstants.EndSessionRequest.PostLogoutRedirectUri, postLogoutRedirectUri ?? _identityServcerClientOptions.PostLogoutRedirectUri ?? string.Empty }
        });

        return uri;
    }


}
