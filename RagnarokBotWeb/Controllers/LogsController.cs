using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Globalization;

namespace RagnarokBotWeb.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthorizationPolicyConstants.AccessTokenPolicy)]
    [Route("api/logs")]
    public class LogsController : ControllerBase
    {
        private readonly ILogger<LogsController> _logger;
        private readonly IServerService _serverService;
        private readonly IFtpService _ftpService;
        private readonly IUnitOfWork _unitOfWork;

        public LogsController(
            ILogger<LogsController> logger,
            IServerService serverService,
            IFtpService ftpService,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _serverService = serverService;
            _ftpService = ftpService;
            _unitOfWork = unitOfWork;
        }

        private GenericLogValue ParseGenericLog(ScumServer server, string line)
        {
            var dateString = line.Substring(0, line.IndexOf(':'));
            string format = "yyyy.MM.dd-HH.mm.ss";
            var date = DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture);
            //date = TimeZoneInfo.ConvertTimeFromUtc(date, server.GetTimeZoneOrDefault());
            return new GenericLogValue { Date = date, Line = line };
        }

        [HttpGet("kills")]
        public async Task<IActionResult> GetKills(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for kills");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            var lastLine = string.Empty;
            List<PreParseKill> preKills = [];
            var parser = new KillLogParser(server);
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Kill, _ftpService, from, to))
            {
                if (!line.Contains('{'))
                {
                    lastLine = line;
                    continue;
                }

                PreParseKill? kill = parser.KillParse(lastLine, line);
                if (kill is null) continue;
                preKills.Add(kill);
            }

            return Ok(preKills);
        }

        [HttpGet("lockpicks")]
        public async Task<IActionResult> GetLockpicks(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for lockpicks");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            List<LockpickLog> lockpicks = [];
            var parser = new LockpickLogParser(server);
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Gameplay, _ftpService, from, to))
            {
                if (line.Contains("[LogMinigame] [LockpickingMinigame_C]") ||
                    line.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
                {

                    LockpickLog? lockpick = parser.Parse(line);
                    if (lockpick is null) continue;
                    lockpicks.Add(lockpick);
                }

            }

            return Ok(lockpicks);
        }

        [HttpGet("economy")]
        public async Task<IActionResult> GetEconomy(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for economy");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Economy, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(server, line));
            }

            return Ok(log);
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for vehicles");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Vehicle_Destruction, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(server, line));
            }

            return Ok(log);
        }

        [HttpGet("login")]
        public async Task<IActionResult> GetLogin(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for login");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Login, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(server, line));
            }

            return Ok(log);
        }

        [HttpGet("buried-chests")]
        public async Task<IActionResult> GetBuriedChests(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for buried chests");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Gameplay, _ftpService, from, to))
            {
                if (line.Contains("[LogChest]"))
                {
                    log.Add(ParseGenericLog(server, line));
                }
            }

            return Ok(log);
        }

        [HttpGet("violations")]
        public async Task<IActionResult> GetViolations(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for violations");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Violations, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(server, line));
            }

            return Ok(log);
        }

        [HttpGet("chat")]
        public async Task<IActionResult> GetChatLogs(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for chat");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Chat, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(server, line));
            }

            return Ok(log);
        }

        [HttpGet("traps")]
        public async Task<IActionResult> GetTrapLogs(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for traps");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server, _unitOfWork);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Gameplay, _ftpService, from, to))
            {
                if (line.Contains("[LogTrap]"))
                {
                    log.Add(ParseGenericLog(server, line));
                }
            }

            return Ok(log);
        }
    }
}
