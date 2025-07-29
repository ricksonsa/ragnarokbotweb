using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class CloseWarzoneJob : AbstractJob, IJob
    {
        private readonly ILogger<CloseWarzoneJob> _logger;
        private readonly IBotRepository _botRepository;
        private readonly IWarzoneService _warzoneService;

        public CloseWarzoneJob(
          IScumServerRepository scumServerRepository,
          IBotRepository botRepository,
          ILogger<CloseWarzoneJob> logger,
          IWarzoneService warzoneService) : base(scumServerRepository)
        {
            _botRepository = botRepository;
            _logger = logger;
            _warzoneService = warzoneService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            if ((await _botRepository.FindByOnlineScumServerId(server.Id)) is null) return;

            var warzoneId = GetValueFromContext<long>(context, "warzone_id");
            if (warzoneId == 0) return;

            await _warzoneService.CloseWarzone(server, context.CancellationToken);
        }
    }
}
