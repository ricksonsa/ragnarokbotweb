using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class EconomyJob : AbstractJob, IJob
{
    private readonly IFtpService _ftpService;
    private readonly ILogger<EconomyJob> _logger;
    private readonly IPlayerService _playerService;
    private readonly IServiceProvider _serviceProvider;

    public EconomyJob(
        ILogger<EconomyJob> logger,
        IPlayerService playerService,
        IFtpService ftpService,
        IServiceProvider serviceProvider,
        IScumServerRepository scumServerRepository
    ) : base(scumServerRepository)
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
            _logger.LogInformation("Triggered EconomyJob->Execute at: {time}", DateTimeOffset.Now);

            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(_serviceProvider, _ftpService, server, fileType);
            var lines = await processor.ProcessUnreadFileLines();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (line.Contains("Game version")) continue;

                if (line.Contains("changed their name"))
                {
                    var (steamId64, scumId, changedName) = new ChangeNameLogParser().Parse(line);
                    await _playerService.PlayerConnected(server, steamId64, scumId, changedName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}