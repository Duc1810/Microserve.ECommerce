

namespace Common.Fakes;

public class FakePasswordTokenResponse
{
    public bool IsError { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public int ExpiresIn { get; set; }
}

