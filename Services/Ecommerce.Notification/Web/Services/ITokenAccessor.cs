namespace Web.Services
{
    using Microsoft.AspNetCore.Http;

    public interface ITokenAccessor
    {
        string? GetAccessToken();
    }

    public sealed class CookieTokenAccessor(IHttpContextAccessor accessor) : ITokenAccessor
    {
        public string? GetAccessToken()
            => accessor.HttpContext?.Request.Cookies["access_token"];
    }

}
