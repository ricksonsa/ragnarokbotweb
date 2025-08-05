using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
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

        public LogsController(
            ILogger<LogsController> logger,
            IServerService serverService,
            IFtpService ftpService)
        {
            _logger = logger;
            _serverService = serverService;
            _ftpService = ftpService;
        }

        //[HttpGet("kills-async")]
        //public async Task GetKillsAsync(DateTime from, DateTime to, string? filter)
        //{
        //    Response.ContentType = "application/json";
        //    _logger.Log(LogLevel.Debug, "REST Request to fetch all logs for kills");
        //    var server = await _serverService.GetServer();
        //    var processor = new ScumFileProcessor(server);

        //    var lastLine = string.Empty;
        //    await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Kill, _ftpService, from, to))
        //    {
        //        if (!line.Contains('{'))
        //        {
        //            lastLine = line;
        //            continue;
        //        }

        //        PreParseKill? kill = KillLogParser.KillParse(lastLine, line);
        //        if (kill is null) continue;

        //        if (!string.IsNullOrEmpty(filter))
        //        {
        //            if (!kill.Weapon.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        //                || !kill.Victim.ProfileName.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        //                || !kill.Victim.UserId.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        //                || !kill.Killer.ProfileName.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        //                || !kill.Killer.UserId.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        //                || !kill.TimeOfDay.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        //                ) continue;
        //        }

        //        var buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(kill));
        //        await Response.Body.WriteAsync(buffer);
        //        await Response.Body.FlushAsync();
        //    }
        //}

        private GenericLogValue ParseGenericLog(string line)
        {
            var dateString = line.Substring(0, line.IndexOf(":"));
            string format = "yyyy.MM.dd-HH.mm.ss";
            var date = DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture);
            return new GenericLogValue { Date = date, Line = line };
        }

        [HttpGet("kills")]
        public async Task<IActionResult> GetKills(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for kills");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server);

            var lastLine = string.Empty;
            List<PreParseKill> preKills = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Kill, _ftpService, from, to))
            {
                if (!line.Contains('{'))
                {
                    lastLine = line;
                    continue;
                }

                PreParseKill? kill = KillLogParser.KillParse(lastLine, line);
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
            var processor = new ScumFileProcessor(server);

            List<LockpickLog> lockpicks = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Gameplay, _ftpService, from, to))
            {
                if (line.Contains("[LogMinigame] [LockpickingMinigame_C]") ||
                    line.Contains("[LogMinigame] [BP_DialLockMinigame_C]"))
                {

                    LockpickLog? lockpick = LockpickLogParser.Parse(line);
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
            var processor = new ScumFileProcessor(server);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Economy, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(line));
            }

            return Ok(log);
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for vehicles");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Vehicle_Destruction, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(line));
            }

            return Ok(log);
        }

        [HttpGet("login")]
        public async Task<IActionResult> GetLogin(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for login");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Login, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(line));
            }

            return Ok(log);
        }

        [HttpGet("buried-chests")]
        public async Task<IActionResult> GetBuriedChests(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for buried chests");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Gameplay, _ftpService, from, to))
            {
                if (line.Contains("[LogChest]"))
                {
                    log.Add(ParseGenericLog(line));
                }
            }

            return Ok(log);
        }

        [HttpGet("violations")]
        public async Task<IActionResult> GetViolations(DateTime from, DateTime to)
        {
            _logger.Log(LogLevel.Debug, "REST Request to fetch logs for violations");
            var server = await _serverService.GetServer();
            var processor = new ScumFileProcessor(server);

            List<GenericLogValue> log = [];
            await foreach (var line in processor.FileLinesAsync(Domain.Enums.EFileType.Violations, _ftpService, from, to))
            {
                log.Add(ParseGenericLog(line));
            }

            return Ok(log);
        }
    }
}
