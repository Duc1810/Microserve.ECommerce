namespace Authentication.Application.Abstractions
{
    public interface ILogoutService
    {

        Task<bool> LogoutAsync(string? refreshToken, CancellationToken ct);
        
        Task<string> BuildEndSessionUrlAsync(string? idTokenHint, string? postLogoutRedirectUri, CancellationToken ct);
    }
}
