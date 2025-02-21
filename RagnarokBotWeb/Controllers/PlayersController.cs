using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Route("api/public/players")]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;

        public PlayersController(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        [HttpGet("state")]
        public IActionResult IsConnected(string steamid)
        {
            return Ok(new { state = _playerService.IsPlayerConnected(steamid) ? "online" : "offline" });
        }

        [HttpGet("state/reset")]
        public IActionResult ResetPlayersState()
        {
            _playerService.ResetPlayersConnection();
            return Ok();
        }
    }
}
