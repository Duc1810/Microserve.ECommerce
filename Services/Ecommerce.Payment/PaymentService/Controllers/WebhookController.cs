using Microsoft.AspNetCore.Mvc;
using PaymentService.Services;
using PayOS.Models.Webhooks;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IPayOSService _payOSService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IPayOSService payOSService,
        ILogger<WebhookController> logger)
    {
        _payOSService = payOSService;
        _logger = logger;
    }

    /// <summary>
    /// Handle PayOS webhook notifications
    /// </summary>
    /// <param name="webhook">Webhook data from PayOS</param>
    /// <returns>Processing result</returns>
    [HttpPost("payos")]
    public async Task<IActionResult> HandlePayOSWebhook([FromBody] Webhook webhook)
    {
        try
        {
            _logger.LogInformation("Received PayOS webhook");

            var result = await _payOSService.ProcessWebhookAsync(webhook);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully processed PayOS webhook");
                return Ok(new { success = true, message = result.Message });
            }
            else
            {
                _logger.LogWarning("Failed to process PayOS webhook: {Error}", result.Message);
                return StatusCode((int)result.StatusCode, new 
                { 
                    success = false, 
                    error = result.Code, 
                    message = result.Message 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing PayOS webhook");
            return StatusCode(500, new 
            { 
                success = false, 
                error = "INTERNAL_ERROR", 
                message = "An unexpected error occurred" 
            });
        }
    }

    /// <summary>
    /// Health check endpoint for webhook
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}