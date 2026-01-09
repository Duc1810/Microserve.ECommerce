using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Web.Pages.Auth;

public class DevLoginModel : PageModel
{
    public void OnGet() { }

    public record TokenPayload(string accessToken);

    [ValidateAntiForgeryToken] // giữ anti-forgery (khuyến nghị)
    public async Task<IActionResult> OnPostSetCookie()
    {
        // Razor Pages không bind [FromBody] cho handler -> tự đọc JSON
        var payload = await JsonSerializer.DeserializeAsync<TokenPayload>(Request.Body);
        if (payload is null || string.IsNullOrWhiteSpace(payload.accessToken))
            return BadRequest("Missing accessToken");

        Response.Cookies.Append("access_token", payload.accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return new OkResult();
    }
}
