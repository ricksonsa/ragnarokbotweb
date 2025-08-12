using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Interfaces;
using System.Text.Json;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaymentService _paymentService;
        private readonly PayPalService _payPalService;

        public PaymentController(ILogger<PaymentController> logger, IPaymentService paymentService, PayPalService payPalService)
        {
            _logger = logger;
            _paymentService = paymentService;
            _payPalService = payPalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPacks([FromQuery] Paginator paginator)
        {
            _logger.LogDebug("GET Request to fetch a list of payments");
            return Ok(await _paymentService.GetPayments(paginator));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            _logger.LogDebug("GET Request to fetch a payment by id {Id}", id);
            return Ok(await _paymentService.GetPayment(id));
        }

        [HttpPost()]
        public async Task<IActionResult> CreatePayment()
        {
            _logger.LogDebug("POST Request to create a payment");
            return Ok(await _paymentService.AddPayment());
        }

        [HttpGet("success")]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string token, [FromQuery] string payerID)
        {
            try
            {
                // Capturar o pagamento
                //var captureResult = await _payPalService.CaptureOrderAsync(token);
                return Ok(await _paymentService.ConfirmPayment());
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("cancel")]
        public IActionResult PaymentCancel(string token)
        {
            return Ok(new { message = "Canceled by the user." });
        }

        [HttpDelete("{id}")]
        public IActionResult PaymentCancelById(long id)
        {

            return Ok(_paymentService.CancelPayment(id));
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> PayPalWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var webhookPayload = await reader.ReadToEndAsync();

                var webhookEvent = JsonSerializer.Deserialize<dynamic>(webhookPayload);

                // Processar diferentes tipos de eventos
                // PAYMENT.CAPTURE.COMPLETED, PAYMENT.CAPTURE.DENIED, etc.

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
