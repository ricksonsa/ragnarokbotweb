using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Route("api/admin/players")]
    [Authorize]
    public class AdminPlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;

        public AdminPlayersController(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        [HttpGet("online")]
        public IActionResult OnlinePlayers()
        {
            return Ok(_playerService.OnlinePlayers());
        }

        [HttpGet("offline")]
        public async Task<IActionResult> OfflinePlayers()
        {
            return Ok(await _playerService.OfflinePlayers());
        }
    }
}
