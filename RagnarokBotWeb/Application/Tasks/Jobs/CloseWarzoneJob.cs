using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class CloseWarzoneJob : AbstractJob, IJob
    {
        private readonly ILogger<CloseWarzoneJob> _logger;
        private readonly IBotService _botService;
        private readonly IWarzoneService _warzoneService;

        public CloseWarzoneJob(
          IScumServerRepository scumServerRepository,
          IBotService botService,
          ILogger<CloseWarzoneJob> logger,
          IWarzoneService warzoneService) : base(scumServerRepository)
        {
            _botService = botService;
            _logger = logger;
            _warzoneService = warzoneService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            if (!_botService.IsBotOnline(server.Id)) return;

            var warzoneId = GetValueFromContext<long>(context, "warzone_id");
            if (warzoneId == 0) return;

            await _warzoneService.CloseWarzone(server, context.CancellationToken);
        }
    }
}
