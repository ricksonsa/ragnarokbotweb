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

        [HttpGet("vip-count")]
        public async Task<IActionResult> GetVipCount()
        {
            var count = await _playerService.GetVipCount();
            return Ok(new
            {
                Count = count
            });
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _playerService.GetCount();
            return Ok(new
            {
                Count = count
            });
        }

        [HttpGet("whitelist-count")]
        public async Task<IActionResult> GetWhitelistCount()
        {
            var count = await _playerService.GetWhitelistCount();
            return Ok(new
            {
                Count = count
            });
        }

        [HttpGet("vip")]
        public async Task<IActionResult> GetVipPlayers([FromQuery] Paginator paginator, string? filter)
        {
            var players = await _playerService.GetVipPlayers(paginator, filter);
            return Ok(players);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlayer(long id)
        {
            var player = await _playerService.GetPlayer(id);
            return Ok(player);
        }

        [HttpGet("steam/{id}")]
        public async Task<IActionResult> GetPlayerBySteam(string id)
        {
            var player = await _playerService.GetPlayerBySteamId(id);
            return Ok(player);
        }

        [HttpGet("statistics/monthly-registers")]
        public async Task<IActionResult> GetPlayerMonthly()
        {
            var players = await _playerService.NewPlayersPerMonth();
            return Ok(players);
        }

        [HttpGet("statistics/kills")]
        public async Task<IActionResult> GetKillStatistics()
        {
            var players = await _playerService.KillRank();
            return Ok(players);
        }

        [HttpGet("statistics/lockpicks")]
        public async Task<IActionResult> GetLockpickStatistics()
        {
            var players = await _playerService.LockpickRank();
            return Ok(players);
        }

        [HttpPatch("coins")]
        public async Task<IActionResult> UpdatePlayerCoins(bool online, ChangeAmountDto dto)
        {
            await _playerService.UpdateCoinsToAll(online, dto);
            return Ok();
        }

        [HttpPatch("{id}/coins")]
        public async Task<IActionResult> UpdatePlayerCoins(long id, ChangeAmountDto dto)
        {
            var player = await _playerService.UpdateCoins(id, dto);
            return Ok(player);
        }

        [HttpPatch("{id}/fame")]
        public async Task<IActionResult> UpdatePlayerFame(long id, ChangeAmountDto dto)
        {
            var player = await _playerService.UpdateFame(id, dto);
            return Ok(player);
        }

        [HttpPatch("{id}/gold")]
        public async Task<IActionResult> UpdatePlayerGold(long id, ChangeAmountDto dto)
        {
            var player = await _playerService.UpdateGold(id, dto);
            return Ok(player);
        }

        [HttpPatch("{id}/money")]
        public async Task<IActionResult> UpdatePlayerMoney(long id, ChangeAmountDto dto)
        {
            var player = await _playerService.UpdateMoney(id, dto);
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
