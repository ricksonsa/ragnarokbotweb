using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Route("api/admin/players")]
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
    }
}
