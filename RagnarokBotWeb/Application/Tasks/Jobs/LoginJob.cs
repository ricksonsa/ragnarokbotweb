using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class LoginJob : FtpJob, IJob
    {
        private readonly ILogger<LoginJob> _logger;
        private readonly IPlayerService _playerService;

        public LoginJob(
            ILogger<LoginJob> logger,
            IFtpService ftpService,
            IReaderRepository reader,
            IScumServerRepository scumServerRepository,
            IPlayerService playerService) : base(reader, ftpService, scumServerRepository, "login_")
        {
            _logger = logger;
            _playerService = playerService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Triggered LoginJob->Execute at: {time}", DateTimeOffset.Now);
                var server = await GetServerAsync(context);

                foreach (var fileName in GetLogFiles(server.Ftp!))
                {
                    foreach (var line in await GetUnreadFileLinesAsync(server.Ftp!, fileName))
                    {
                        _logger.LogInformation("LoginJob->Execute Reading file: {}", fileName);

                        if (string.IsNullOrEmpty(line.Value)) continue;

                        var (steamId64, scumId, name, loggedIn) = new LoginLogParser().Parse(line.Value);
                        if (string.IsNullOrWhiteSpace(steamId64)) continue;

                        if (loggedIn)
                        {
                            await _playerService.PlayerConnected(server, steamId64, scumId, name);
                        }
                        else
                        {
                            _playerService.PlayerDisconnected(server.Id, steamId64);
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
