using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.BotServer;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Interfaces;
using System.Text.Json;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/bots")]
    public class BotController : ControllerBase
    {
        private readonly ILogger<BotController> _logger;
        private readonly IBotService _botService;
        private readonly BotSocketServer _botSocketServer;

        public BotController(ILogger<BotController> logger, IBotService botService, BotSocketServer botSocketServer)
        {
            _logger = logger;
            _botService = botService;
            _botSocketServer = botSocketServer;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await Task.Run(_botService.GetBots));
        }

        [HttpGet("count")]
        public IActionResult GetCount()
        {
            return Ok(new
            {
                Value = _botService.GetConnectedBots().Count
            });
        }

        [HttpGet("guid/{guid}")]
        public async Task<IActionResult> GetBotByGuid(string guid)
        {
            return Ok(await _botService.FindBotByGuid(new Guid(guid)));
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

        [HttpPatch("deliveries/{id}/confirm")]
        public async Task<IActionResult> ConfirmDelivery(long id)
        {
            await _botService.ConfirmDelivery(id);
            return Ok();
        }

        // Enhanced endpoint with detailed bot information
        [HttpGet("servers/{serverId}/status")]
        public IActionResult GetBotStatus(long serverId)
        {
            try
            {
                var stats = _botSocketServer.GetBotStats(serverId);

                return Ok(new
                {
                    ServerId = serverId,
                    Timestamp = DateTime.UtcNow,
                    Summary = new
                    {
                        Total = stats.TotalBots,
                        Connected = stats.ConnectedBots,
                        Active = stats.ActiveBots,
                        Inactive = stats.TotalBots - stats.ActiveBots,
                        RestoredFromState = stats.Bots.Count(b => b.RestoredFromState),
                        HasActiveBots = stats.ActiveBots > 0,
                        HasConnectedBots = stats.ConnectedBots > 0
                    },
                    Bots = stats.Bots.Select(bot => new
                    {
                        BotId = bot.Guid.ToString(),
                        SteamId = bot.SteamId,
                        Status = GetBotStatusText(bot),
                        ConnectionStatus = bot.IsConnected ? "Connected" :
                                         bot.RestoredFromState ? "Restored (Disconnected)" : "Disconnected",
                        GameStatus = bot.IsActive ? "Active" : "Inactive",
                        RestoredFromState = bot.RestoredFromState,
                        LastPinged = bot.LastPinged?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Never",
                        LastInteracted = bot.LastInteracted.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        LastCommand = bot.LastCommand?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Never",
                        LastReconnectSent = bot.LastReconnectSent?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Never",
                        TimeSinceLastPing = FormatTimeSpan(bot.MinutesSinceLastPing),
                        TimeSinceLastInteraction = FormatTimeSpan(bot.MinutesSinceLastInteraction),
                        Health = GetBotHealth(bot)
                    }).OrderByDescending(b => b.Status == "Active")
                      .ThenByDescending(b => b.ConnectionStatus == "Connected")
                      .ThenBy(b => b.TimeSinceLastInteraction)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bot status for server {ServerId}", serverId);
                return StatusCode(500, new { Error = "Failed to retrieve bot status", Details = ex.Message });
            }
        }

        // Force reconnect for specific bot
        [HttpPost("{botId}/reconnect")]
        public async Task<IActionResult> ForceReconnect(string botId)
        {
            var serverId = HttpContext?.User?.FindFirst(ClaimConstants.ServerId)?.Value;
            if (serverId == null) throw new UnauthorizedAccessException();

            try
            {
                await _botSocketServer.SendCommandAsync(long.Parse(serverId), botId, new Shared.Models.BotCommand().Reconnect());
                _logger.LogInformation("Manual reconnect command sent to bot {BotId} on server {ServerId}", botId, serverId);

                return Ok(new
                {
                    Message = $"Reconnect command sent to bot {botId}",
                    BotId = botId,
                    ServerId = serverId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reconnect command to bot {BotId} on server {ServerId}", botId, serverId);
                return StatusCode(500, new { Error = "Failed to send reconnect command", Details = ex.Message });
            }
        }

        [HttpGet("{botId}")]
        public IActionResult GetBot(string botId)
        {
            var serverId = HttpContext?.User?.FindFirst(ClaimConstants.ServerId)?.Value;
            if (serverId == null) throw new UnauthorizedAccessException();

            try
            {
                var bots = _botSocketServer.GetBots(long.Parse(serverId));
                var now = DateTime.UtcNow;

                return Ok(bots.Select(bot => new
                {
                    BotId = bot.Guid.ToString(),
                    SteamId = bot.SteamId ?? "Unknown",
                    Connected = bot.TcpClient?.Connected == true,
                    GameActive = bot.LastPinged.HasValue && (now - bot.LastPinged.Value).TotalMinutes < 5,
                    LastSeen = (bot.LastPinged ?? bot.LastInteracted).ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    MinutesSinceLastSeen = bot.LastPinged.HasValue
                        ? Math.Round((now - bot.LastPinged.Value).TotalMinutes, 1)
                        : Math.Round((now - bot.LastInteracted).TotalMinutes, 1)
                }).FirstOrDefault(bot => bot.BotId == botId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reconnect command to bot {BotId} on server {ServerId}", botId, serverId);
                return StatusCode(500, new { Error = "Failed to send reconnect command", Details = ex.Message });
            }
        }


        // Simple endpoint for quick checks
        [HttpGet("table")]
        public IActionResult GetSimpleBotList()
        {
            var serverId = HttpContext?.User?.FindFirst(ClaimConstants.ServerId)?.Value;
            if (serverId == null) throw new UnauthorizedAccessException();

            try
            {
                var bots = _botSocketServer.GetBots(long.Parse(serverId));
                var now = DateTime.UtcNow;

                return Ok(bots.Select(bot => new
                {
                    BotId = bot.Guid.ToString(),
                    SteamId = bot.SteamId ?? "Unknown",
                    Connected = bot.TcpClient?.Connected == true,
                    GameActive = bot.LastPinged.HasValue && (now - bot.LastPinged.Value).TotalMinutes < 5,
                    LastSeen = (bot.LastPinged ?? bot.LastInteracted).ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    MinutesSinceLastSeen = bot.LastPinged.HasValue
                        ? Math.Round((now - bot.LastPinged.Value).TotalMinutes, 1)
                        : Math.Round((now - bot.LastInteracted).TotalMinutes, 1)
                }).OrderBy(b => b.MinutesSinceLastSeen).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simple bot list for server {ServerId}", serverId);
                return StatusCode(500, new { Error = "Failed to retrieve bot list", Details = ex.Message });
            }
        }

        // Simple endpoint for quick checks
        [HttpGet("servers/{serverId}/bots")]
        public IActionResult GetSimpleBotList(long serverId)
        {
            try
            {
                var bots = _botSocketServer.GetBots(serverId);
                var now = DateTime.UtcNow;

                return Ok(bots.Select(bot => new
                {
                    BotId = bot.Guid.ToString(),
                    SteamId = bot.SteamId ?? "Unknown",
                    Connected = bot.TcpClient?.Connected == true,
                    GameActive = bot.LastPinged.HasValue && (now - bot.LastPinged.Value).TotalMinutes < 5,
                    LastSeen = (bot.LastPinged ?? bot.LastInteracted).ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    MinutesSinceLastSeen = bot.LastPinged.HasValue
                        ? Math.Round((now - bot.LastPinged.Value).TotalMinutes, 1)
                        : Math.Round((now - bot.LastInteracted).TotalMinutes, 1)
                }).OrderBy(b => b.MinutesSinceLastSeen).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting simple bot list for server {ServerId}", serverId);
                return StatusCode(500, new { Error = "Failed to retrieve bot list", Details = ex.Message });
            }
        }

        // Check if any bots are available
        [HttpGet("servers/{serverId}/available")]
        public IActionResult IsBotAvailable(long serverId)
        {
            try
            {
                var isConnected = _botSocketServer.IsBotConnected(serverId);
                var stats = _botSocketServer.GetBotStats(serverId);

                return Ok(new
                {
                    ServerId = serverId,
                    Available = isConnected,
                    ConnectedBots = stats.ConnectedBots,
                    ActiveBots = stats.ActiveBots,
                    Message = isConnected
                        ? $"{stats.ActiveBots} active bot(s) available"
                        : "No bots available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking bot availability for server {ServerId}", serverId);
                return StatusCode(500, new { Error = "Failed to check bot availability", Details = ex.Message });
            }
        }

        // Get overview of all servers
        [HttpGet("overview")]
        public IActionResult GetAllServersOverview()
        {
            try
            {
                var allBots = _botSocketServer.GetAllServersStats();

                return Ok(new
                {
                    Timestamp = DateTime.UtcNow,
                    TotalServers = allBots.Count,
                    Servers = allBots.Select(serverStats => new
                    {
                        ServerId = serverStats.ServerId,
                        TotalBots = serverStats.TotalBots,
                        ConnectedBots = serverStats.ConnectedBots,
                        ActiveBots = serverStats.ActiveBots,
                        Status = serverStats.ActiveBots > 0 ? "Active" :
                                 serverStats.ConnectedBots > 0 ? "Connected" : "Offline"
                    }).OrderByDescending(s => s.ActiveBots).ThenBy(s => s.ServerId)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all servers overview");
                return StatusCode(500, new { Error = "Failed to retrieve servers overview", Details = ex.Message });
            }
        }

        // Force reconnect for specific bot
        [HttpPost("servers/{serverId}/bots/{botId}/reconnect")]
        public async Task<IActionResult> ForceReconnect(long serverId, string botId)
        {
            try
            {
                await _botSocketServer.SendCommandAsync(serverId, botId, new Shared.Models.BotCommand().Reconnect());
                _logger.LogInformation("Manual reconnect command sent to bot {BotId} on server {ServerId}", botId, serverId);

                return Ok(new
                {
                    Message = $"Reconnect command sent to bot {botId}",
                    BotId = botId,
                    ServerId = serverId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reconnect command to bot {BotId} on server {ServerId}", botId, serverId);
                return StatusCode(500, new { Error = "Failed to send reconnect command", Details = ex.Message });
            }
        }

        // Force reconnect for all bots on server
        [HttpPost("servers/{serverId}/reconnect-all")]
        public async Task<IActionResult> ForceReconnectAll(long serverId)
        {
            try
            {
                await _botSocketServer.SendCommandToAll(serverId, new Shared.Models.BotCommand().Reconnect());
                _logger.LogInformation("Manual reconnect command sent to all bots on server {ServerId}", serverId);

                return Ok(new
                {
                    Message = $"Reconnect command sent to all bots on server {serverId}",
                    ServerId = serverId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reconnect command to all bots on server {ServerId}", serverId);
                return StatusCode(500, new { Error = "Failed to send reconnect command to all bots", Details = ex.Message });
            }
        }

        // Force save bot state manually
        [HttpPost("state/save")]
        public IActionResult SaveBotState()
        {
            try
            {
                _botSocketServer.SaveBotStateOnShutdown(); // Reuse the same method
                _logger.LogInformation("Manual bot state save requested");

                return Ok(new
                {
                    Message = "Bot state saved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bot state manually");
                return StatusCode(500, new { Error = "Failed to save bot state", Details = ex.Message });
            }
        }

        // Get state file information
        [HttpGet("state/info")]
        public IActionResult GetStateInfo()
        {
            try
            {
                var stateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot_state.json");

                if (!System.IO.File.Exists(stateFilePath))
                {
                    return Ok(new
                    {
                        StateFileExists = false,
                        FilePath = stateFilePath,
                        Message = "No state file found"
                    });
                }

                var fileInfo = new FileInfo(stateFilePath);
                var content = System.IO.File.ReadAllText(stateFilePath);

                BotServerState? stateData = null;
                try
                {
                    stateData = JsonSerializer.Deserialize<BotServerState>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse state file content");
                }

                return Ok(new
                {
                    StateFileExists = true,
                    FilePath = stateFilePath,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    SavedAt = stateData?.SavedAt.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Unknown",
                    ServersCount = stateData?.Servers?.Count ?? 0,
                    TotalBotsInFile = stateData?.Servers?.Sum(s => s.Value.Count) ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting state info");
                return StatusCode(500, new { Error = "Failed to get state info", Details = ex.Message });
            }
        }

        private static string GetBotStatusText(BotStatus bot)
        {
            if (bot.IsConnected && bot.IsActive)
                return "Active";
            if (bot.IsConnected && !bot.IsActive)
                return "Connected";
            if (!bot.IsConnected && bot.MinutesSinceLastPing.HasValue && bot.MinutesSinceLastPing < 10)
                return "Recently Active";

            return "Offline";
        }

        private static string FormatTimeSpan(double? minutes)
        {
            if (!minutes.HasValue)
                return "Never";

            var totalMinutes = minutes.Value;

            if (totalMinutes < 1)
                return "< 1 minute";
            if (totalMinutes < 60)
                return $"{Math.Round(totalMinutes)} minutes";
            if (totalMinutes < 1440) // 24 hours
                return $"{Math.Round(totalMinutes / 60, 1)} hours";

            return $"{Math.Round(totalMinutes / 1440, 1)} days";
        }

        private static string GetBotHealth(BotStatus bot)
        {
            if (!bot.IsConnected)
                return "Disconnected";

            if (!bot.LastPinged.HasValue)
                return "No Game Activity";

            if (bot.MinutesSinceLastPing < 2)
                return "Excellent";
            if (bot.MinutesSinceLastPing < 5)
                return "Good";
            if (bot.MinutesSinceLastPing < 10)
                return "Warning";

            return "Critical";
        }
    }
}
