using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/players")]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;

        public PlayersController(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        //[HttpGet("state")]
        //public IActionResult IsConnected(string steamid)
        //{
        //    return Ok(new { state = _playerService.IsPlayerConnected(steamid) ? "online" : "offline" });
        //}

        //[HttpGet("state/reset")]
        //public IActionResult ResetPlayersState()
        //{
        //    _playerService.ResetPlayersConnection();
        //    return Ok();
        //}

        [HttpGet]
        public async Task<IActionResult> GetPlayers([FromQuery] Paginator paginator, string? filter)
        {
            var players = await _playerService.GetPlayers(paginator, filter);
            return Ok(players);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlayer(long id)
        {
            var player = await _playerService.GetPlayer(id);
            return Ok(player);
        }
    }
}
