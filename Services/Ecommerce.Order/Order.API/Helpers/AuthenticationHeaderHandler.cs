namespace Order.API.Helpers;
public class AuthenticationHeaderHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Get the current HTTP Context
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // 2. Extract the "Authorization" header from the incoming request
            string? authorizationHeader = httpContext.Request.Headers["Authorization"];

            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                // 3. Forward the token to the outgoing request
                request.Headers.Add("Authorization", authorizationHeader);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

