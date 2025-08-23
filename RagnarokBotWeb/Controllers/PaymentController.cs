using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Interfaces;

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

        [HttpGet("token/{token}")]
        public async Task<IActionResult> GetById(string token)
        {
            _logger.LogDebug("GET Request to fetch a payment by token {Token}", token);
            return Ok(await _paymentService.GetPayment(token));
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

        //[HttpPost("webhook")]
        //public async Task<IActionResult> PaymentSuccess([FromQuery] string token, [FromQuery] string payerID)
        //{
        //    try
        //    {
        //        // Capturar o pagamento
        //        //var captureResult = await _payPalService.CaptureOrderAsync(token);
        //        return Ok(await _paymentService.ConfirmPayment());
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //}

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

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> PayPalWebhook([FromBody] FastSpringWebhookCompleted data)
        {
            try
            {
                if (data.Events.Any(e => e.Type == "order.completed"))
                {
                    var eventData = data.Events.First(e => e.Type == "order.completed").Data;
                    await _paymentService.ConfirmPayment(eventData!.Order, eventData.Account);
                }

                //Processar diferentes tipos de eventos
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
