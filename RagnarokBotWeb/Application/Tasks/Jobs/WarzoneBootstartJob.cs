using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class WarzoneBootstartJob : AbstractJob, IJob
    {
        private readonly ILogger<WarzoneBootstartJob> _logger;
        private readonly IBotRepository _botRepository;
        private readonly IWarzoneService _warzoneService;

        public WarzoneBootstartJob(
          IScumServerRepository scumServerRepository,
          IBotRepository botRepository,
          IWarzoneService warzoneService,
          ILogger<WarzoneBootstartJob> logger) : base(scumServerRepository)
        {
            _botRepository = botRepository;
            _warzoneService = warzoneService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered {} -> Execute at: {time}", nameof(WarzoneBootstartJob), DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            if ((await _botRepository.FindByOnlineScumServerId(server.Id)) is null) return;
            await _warzoneService.OpenWarzone(server, context.CancellationToken);
        }
    }
}
