using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class UpdateServerDataJob : AbstractJob, IJob
    {
        private readonly ILogger<UpdateServerDataJob> _logger;
        private readonly IServerService _serverService;

        public UpdateServerDataJob(
            ILogger<UpdateServerDataJob> logger,
            IServerService serverService,
            IScumServerRepository scumServerRepository
            ) : base(scumServerRepository)
        {
            _logger = logger;
            _serverService = serverService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            var server = await GetServerAsync(context, ftpRequired: true);
            await _serverService.UpdateServerData(server);
        }
    }
}
