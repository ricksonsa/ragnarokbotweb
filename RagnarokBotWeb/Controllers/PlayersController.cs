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

        [HttpPost("{id}/silence")]
        public async Task<IActionResult> AddPlayerSilence(long id, PlayerVipDto dto)
        {
            var player = await _playerService.AddSilence(id, dto);
            return Ok(player);
        }

        [HttpPost("{id}/ban")]
        public async Task<IActionResult> AddPlayerBan(long id, PlayerVipDto dto)
        {
            var player = await _playerService.AddBan(id, dto);
            return Ok(player);
        }

        [HttpPost("{id}/vip")]
        public async Task<IActionResult> AddPlayerVip(long id, PlayerVipDto dto)
        {
            var player = await _playerService.AddVip(id, dto);
            return Ok(player);
        }

        [HttpDelete("{id}/ban")]
        public async Task<IActionResult> RemovePlayerBan(long id)
        {
            var player = await _playerService.RemoveBan(id);
            return Ok(player);
        }

        [HttpDelete("{id}/silence")]
        public async Task<IActionResult> RemovePlayerSilence(long id)
        {
            var player = await _playerService.RemoveSilence(id);
            return Ok(player);
        }

        [HttpDelete("{id}/vip")]
        public async Task<IActionResult> RemovePlayerVip(long id)
        {
            var player = await _playerService.RemoveVip(id);
            return Ok(player);
        }
    }
}
