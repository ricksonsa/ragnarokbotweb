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
            _logger.LogInformation("Get request to fetch server data for serverId {Id}", serverId);
            ScumServer server = await _serverService.GetServer(serverId);
            return Ok(server);
        }

        [HttpPatch("discord/config")]
        public async Task<IActionResult> ConfirmDiscord(SaveDiscordSettingsDto settings)
        {
            _logger.LogInformation("Patch request to confirm discord settings");
            GuildDto guild = await _serverService.ConfirmDiscordToken(settings);
            return Ok(guild);
        }

        [HttpPatch("discord/channels/run-template")]
        public async Task<IActionResult> RunDiscordTemplate()
        {
            _logger.LogInformation("Patch request to run default discord templates");
            GuildDto guild = await _serverService.RunDiscordTemplate();
            return Ok(guild);
        }

        [HttpGet("discord")]
        public async Task<IActionResult> GetDiscord()
        {
            _logger.LogInformation("Patch request to confirm discord settings");
            GuildDto guild = await _serverService.GetServerDiscord();
            return Ok(guild);
        }

        [HttpGet("discord/roles")]
        public async Task<IActionResult> GetDiscordRoles()
        {
            _logger.LogInformation("Get request to fetch discord roles");
            var roles = await _serverService.GetServerDiscordRoles();
            return Ok(roles);
        }

        [HttpPut("discord/channels")]
        public async Task<IActionResult> UpdateDiscordChannels(SaveChannelDto channel)
        {
            _logger.LogInformation("Patch request to update discord channels");
            ScumServerDto? server = await _serverService.SaveServerDiscordChannel(channel);
            return Ok(server);
        }

        [HttpGet("discord/channels")]
        public async Task<IActionResult> GetDiscordChannels()
        {
            _logger.LogInformation("Get request to fetch discord channels");
            var channels = await _serverService.GetServerDiscordChannels();
            return Ok(channels);
        }

        [HttpPatch("ftp")]
        public async Task<IActionResult> UpdateServerFtp(FtpDto ftp)
        {
            _logger.LogInformation("Patch request to update ftp data");
            var server = await _serverService.ChangeFtp(ftp);
            return Ok(server);
        }
    }
}
