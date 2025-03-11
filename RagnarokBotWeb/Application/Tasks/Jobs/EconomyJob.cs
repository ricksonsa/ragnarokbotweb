using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class EconomyJob : FtpJob, IJob
    {
        private readonly ILogger<EconomyJob> _logger;
        private readonly IPlayerService _playerService;

        public EconomyJob(
            ILogger<EconomyJob> logger,
            IFtpService ftpService,
            IReaderRepository reader,
            IScumServerRepository scumServerRepository,
            IPlayerService playerService) : base(reader, ftpService, scumServerRepository, "economy_")
        {
            _logger = logger;
            _playerService = playerService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Triggered EconomyJob->Execute at: {time}", DateTimeOffset.Now);
                var server = await GetServerAsync(context);

                foreach (var fileName in GetLogFiles(server.Ftp!))
                {
                    _logger.LogInformation("EconomyJob->Execute Reading file: " + fileName);

                    foreach (var line in await GetUnreadFileLinesAsync(server.Ftp!, fileName))
                    {
                        if (string.IsNullOrEmpty(line.Value)) continue;
                        if (line.Value.Contains("Game version")) continue;

                        if (line.Value.Contains("changed their name"))
                        {
                            var (steamId64, scumId, changedName) = new ChangeNameLogParser().Parse(line.Value);
                            await _playerService.PlayerConnected(server, steamId64, scumId, changedName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
