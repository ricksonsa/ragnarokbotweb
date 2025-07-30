using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class WarzoneBootstartJob : AbstractJob, IJob
    {
        private readonly ILogger<WarzoneBootstartJob> _logger;
        private readonly IBotService _botService;
        private readonly IWarzoneService _warzoneService;

        public WarzoneBootstartJob(
          IScumServerRepository scumServerRepository,
          IBotService botService,
          IWarzoneService warzoneService,
          ILogger<WarzoneBootstartJob> logger) : base(scumServerRepository)
        {
            _botService = botService;
            _warzoneService = warzoneService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            var server = await GetServerAsync(context, ftpRequired: false);

            if (!_botService.IsBotOnline(server.Id)) return;
            await _warzoneService.OpenWarzone(server, context.CancellationToken);
        }
    }
}
