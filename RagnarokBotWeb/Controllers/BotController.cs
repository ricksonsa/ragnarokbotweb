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
        private readonly ICacheService _cacheService;
        private readonly IBotService _botService;

        public BotController(ILogger<BotController> logger, ICacheService cacheService, IBotService botService)
        {
            _logger = logger;
            _cacheService = cacheService;
            _botService = botService;
        }

        [HttpGet("commands")]
        public async Task<IActionResult> GetReadyCommands(string identifier)
        {
            await _botService.UpdateInteraction(identifier);
            if (_cacheService.GetCommandQueue().TryDequeue(out var command))
            {
                return Ok(command);
            }

            return Ok(null);
        }

        [HttpGet("players")]
        public IActionResult UpdatePlayers([FromQuery] string input, [FromQuery] string identifier)
        {
            _botService.UpdatePlayersOnline(input, identifier);
            return Ok();
        }

        [HttpPost("commands")]
        public IActionResult CreateCommand(BotCommand command)
        {
            _cacheService.GetCommandQueue().Enqueue(command);
            return Ok(command);
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterBot(string identifier)
        {
            _logger.LogInformation($"Bot with identifier {identifier} registered");
            var bot = await _botService.RegisterBot(identifier);
            return Ok(bot);
        }

        [HttpDelete("unregister")]
        public async Task<IActionResult> UnregisterBot(string identifier)
        {
            _logger.LogInformation($"Bot with identifier {identifier} unregistered");
            var bot = await _botService.UnregisterBot(identifier);
            return Ok(bot);
        }
    }
}
