using Microsoft.AspNetCore.Mvc;

namespace Dashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Status = "Healthy", Service = "Dashboard API", Timestamp = DateTime.UtcNow });
    }
}