using Microsoft.AspNetCore.Mvc;
using Net.payOS;
using Net.payOS.Types;
using PaymentService.Service;
using PaymentService.Services;
using PayOS.Models.Webhooks;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController(
        IPayOSService paymentService) : ControllerBase
    {
        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] Webhook webhook)
        {

            var result = await paymentService.ProcessWebhookAsync(webhook);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(new { message = "Webhook processed successfully"});
        }
    }
}