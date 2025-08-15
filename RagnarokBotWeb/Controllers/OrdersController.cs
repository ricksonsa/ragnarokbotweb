using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private readonly IOrderService _orderService;

        public OrdersController(ILogger<OrdersController> logger, IOrderService orderService)
        {
            _logger = logger;
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderDto createOrder)
        {
            var order = await _orderService.PlaceDeliveryOrder(createOrder.SteamId, createOrder.PackId);
            if (order is null) return BadRequest("Invalid payload");
            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] Paginator paginator, string? filter)
        {
            _logger.LogDebug("Get request to fetch a page of orders");
            var page = await _orderService.GetPacksPageByFilterAsync(paginator, filter);
            return Ok(page);
        }

        [HttpGet("best-sellers")]
        public async Task<IActionResult> GetBestSellingOrders()
        {
            _logger.LogDebug("Get request to fetch a best sellers");
            var page = await _orderService.GetBestSellingOrdersPacks();
            return Ok(page);
        }

        [HttpPatch("players/{playerId}/welcomepack")]
        public async Task<IActionResult> DeliverWelcomePack(long playerId)
        {
            _logger.LogDebug("Patch request to deliver welcomepack");
            var order = await _orderService.PlaceWelcomePackOrder(playerId);
            return Ok(order);
        }
    }
}
