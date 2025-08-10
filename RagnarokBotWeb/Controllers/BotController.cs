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

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_botService.GetBots());
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterBot(string guid)
        {
            await _botService.RegisterBot(new Guid(guid));
            return Ok();
        }

        [HttpGet("guid/{guid}")]
        public async Task<IActionResult> GetBotByGuid(string guid)
        {
            return Ok(await _botService.FindBotByGuid(new Guid(guid)));
        }

        [HttpGet("commands")]
        public async Task<IActionResult> GetReadyCommands(string guid)
        {
            return Ok(await _botService.GetCommand(new Guid(guid)));
        }

        [HttpPost("players")]
        public async Task<IActionResult> UpdatePlayers(UpdateFromStringRequest input)
        {
            await _botService.UpdatePlayersOnline(input);
            return Ok();
        }

        [HttpPost("squads")]
        public async Task<IActionResult> UpdateSquads(UpdateFromStringRequest input)
        {
            await _botService.UpdateSquads(input);
            return Ok();
        }

        [HttpPost("flags")]
        public async Task<IActionResult> UpdateFlags(UpdateFromStringRequest input)
        {
            await _botService.UpdateFlags(input);
            return Ok();
        }

        [HttpPost("commands")]
        public IActionResult CreateCommand(BotCommand command)
        {
            _botService.PutCommand(command);
            return Ok();
        }


        [HttpPatch("deliveries/{id}/confirm")]
        public async Task<IActionResult> ConfirmDelivery(long id)
        {
            await _botService.ConfirmDelivery(id);
            return Ok();
        }

        [HttpDelete("unregister")]
        public IActionResult UnregisterBot(string guid)
        {
            _botService.DisconnectBot(new Guid(guid));
            return Ok();
        }
    }
}
