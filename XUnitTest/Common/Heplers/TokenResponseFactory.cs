// Tests/Common/Helpers/TokenResponseFactory.cs
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Common.Helpers;

public static class TokenResponseFactory
{
    public static async Task<TokenResponse> OkAsync(
        string accessToken = "access",
        string refreshToken = "refresh",
        string tokenType = "Bearer",
        int expiresIn = 3600)
    {
        var json = $@"{{
            ""access_token"": ""{accessToken}"",
            ""refresh_token"": ""{refreshToken}"",
            ""token_type"": ""{tokenType}"",
            ""expires_in"": {expiresIn}
        }}";

        var msg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(msg);
    }

    public static async Task<TokenResponse> ErrorAsync(
        string error = "invalid_client",
        string errorDescription = "bad secret",
        HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        var json = $@"{{
            ""error"": ""{error}"",
            ""error_description"": ""{errorDescription}""
        }}";

        var msg = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(msg);
    }
}
