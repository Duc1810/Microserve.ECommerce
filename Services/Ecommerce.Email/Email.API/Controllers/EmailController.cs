using BuildingBlocks.Observability.ApiResponse;
using Email.API.Dtos;
using Email.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Email.API.Controllers;
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] EmailRequestDTO request)
    {
        await _emailService.SendEmail(request);
        return Ok(ApiResponse<object>.Ok("Email sent"));
    }


}
