using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/bots")]
    public class BotController : ControllerBase
    {
        private readonly ILogger<BotController> _logger;
        private readonly IBotService _botService;

        public BotController(ILogger<BotController> logger, IBotService botService)
        {
            _logger = logger;
            _botService = botService;
        }

        [HttpGet("commands")]
        public async Task<IActionResult> GetReadyCommands()
        {
            return Ok(await _botService.GetCommand());
        }

        [HttpGet("players")]
        public IActionResult UpdatePlayers([FromQuery] string input)
        {
            _botService.UpdatePlayersOnline(input);
            return Ok();
        }

        [HttpPost("commands")]
        public async Task<IActionResult> CreateCommand(BotCommand command)
        {
            await _botService.PutCommand(command);
            return Ok();
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterBot()
        {
            var bot = await _botService.RegisterBot();
            return Ok(bot);
        }

        [HttpDelete("unregister")]
        public async Task<IActionResult> UnregisterBot()
        {
            var bot = await _botService.UnregisterBot();
            return Ok(bot);
        }
    }
}
