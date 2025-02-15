using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Route("api/bot/commands")]
    public class CommandsController : ControllerBase
    {
        private readonly ILogger<CommandsController> _logger;
        private readonly IOrderService _orderService;

        public CommandsController(ILogger<CommandsController> logger, IOrderService orderService)
        {
            _logger = logger;
            _orderService = orderService;
        }

        [HttpGet("ready")]
        public async Task<IActionResult> GetReadyCommands(long botId)
        {
            return Ok(await _orderService.GetCommand(botId));
        }
    }
}
