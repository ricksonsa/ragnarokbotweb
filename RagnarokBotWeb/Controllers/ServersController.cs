using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/servers")]
    public class ServersController : ControllerBase
    {
        private readonly ILogger<ServersController> _logger;
        private readonly IServerService _serverService;

        public ServersController(ILogger<ServersController> logger, IServerService serverService)
        {
            _logger = logger;
            _serverService = serverService;
        }

        [HttpGet("{serverId}")]
        public async Task<IActionResult> GetServer(long serverId)
        {
            _logger.LogInformation("Get request to fetch server data for serverId {}", serverId);
            ScumServer server = await _serverService.GetServer(serverId);
            return Ok(server);
        }

        [HttpPatch("settings/discord")]
        public async Task<IActionResult> ConfirmDiscord(SaveDiscordSettingsDto settings)
        {
            _logger.LogInformation("Patch request to confirm discord settings");
            GuildDto guild = await _serverService.ConfirmDiscordToken(settings);
            return Ok(guild);
        }

        [HttpPatch("settings/ftp")]
        public async Task<IActionResult> UpdateServerFtp(FtpDto ftp)
        {
            _logger.LogInformation("Patch request to update ftp data");
            var server = await _serverService.ChangeFtp(ftp);
            return Ok(server);
        }
    }
}
