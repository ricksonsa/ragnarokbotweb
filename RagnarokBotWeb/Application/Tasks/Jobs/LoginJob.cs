using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class LoginJob : AbstractJob, IJob
{
    private readonly IFtpService _ftpService;
    private readonly ILogger<LoginJob> _logger;
    private readonly IPlayerService _playerService;
    private readonly IServiceProvider _serviceProvider;

    public LoginJob(
        ILogger<LoginJob> logger,
        IFtpService ftpService,
        IScumServerRepository scumServerRepository,
        IPlayerService playerService,
        IServiceProvider serviceProvider) : base(scumServerRepository)
    {
        _logger = logger;
        _playerService = playerService;
        _ftpService = ftpService;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Triggered LoginJob->Execute at: {time}", DateTimeOffset.Now);
            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(_serviceProvider, _ftpService, server, fileType);
            var lines = await processor.ProcessUnreadFileLines();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;

                var (steamId64, scumId, name, loggedIn) = new LoginLogParser().Parse(line);
                if (string.IsNullOrWhiteSpace(steamId64)) continue;

                if (loggedIn)
                    await _playerService.PlayerConnected(server, steamId64, scumId, name);
                else
                    _playerService.PlayerDisconnected(server.Id, steamId64);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}