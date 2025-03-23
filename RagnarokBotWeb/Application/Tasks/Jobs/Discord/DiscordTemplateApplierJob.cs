using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs.Discord
{
    public class DiscordTemplateApplierJob : AbstractJob, IJob
    {
        private readonly ILogger<DiscordTemplateApplierJob> _logger;
        private readonly IDiscordService _discordService;

        public DiscordTemplateApplierJob(
            IScumServerRepository scumServerRepository,
            ILogger<DiscordTemplateApplierJob> logger,
            IDiscordService discordService) : base(scumServerRepository)
        {
            _logger = logger;
            _discordService = discordService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var serverId = GetServerIdFromContext(context);

            var jobName = context.JobDetail.Key.Name;
            _logger.LogInformation($"Executing job: {jobName} from serverId: {serverId} at {DateTime.Now}");

            await _discordService.CreateChannelTemplates(serverId);
        }


    }
}
