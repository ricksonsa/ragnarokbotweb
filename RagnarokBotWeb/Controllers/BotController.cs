using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
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

        [HttpPost("players")]
        public IActionResult UpdatePlayers(PlayersListRequest input)
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

        [HttpPatch("deliveries/{id}/confirm")]
        public async Task<IActionResult> ConfirmDelivery(long id)
        {
            await _botService.ConfirmDelivery(id);
            return Ok();
        }

        [HttpDelete("unregister")]
        public async Task<IActionResult> UnregisterBot()
        {
            var bot = await _botService.UnregisterBot();
            return Ok(bot);
        }
    }
}
