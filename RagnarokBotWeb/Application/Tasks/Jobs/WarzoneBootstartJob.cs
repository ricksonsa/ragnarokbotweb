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
        private readonly ISchedulerFactory _schedulerFactory;

        public WarzoneBootstartJob(
          IScumServerRepository scumServerRepository,
          IBotService botService,
          IWarzoneService warzoneService,
          ILogger<WarzoneBootstartJob> logger,
          ISchedulerFactory schedulerFactory) : base(scumServerRepository)
        {
            _botService = botService;
            _warzoneService = warzoneService;
            _logger = logger;
            _schedulerFactory = schedulerFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);

            if (!_botService.IsBotOnline(server.Id)) return;

            var scheduler = await _schedulerFactory.GetScheduler();
            if (!await scheduler.CheckExists(new JobKey($"CloseWarzoneJob({server.Id})")))
            {
                await _warzoneService.OpenWarzone(server, token: context.CancellationToken);

            }
        }
    }
}
