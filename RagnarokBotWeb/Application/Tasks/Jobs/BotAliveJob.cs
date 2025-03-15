using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class BotAliveJob : IJob
    {
        private readonly ILogger<BotAliveJob> _logger;
        private readonly IBotService _botService;

        public BotAliveJob(ILogger<BotAliveJob> logger, IBotService botService)
        {
            _logger = logger;
            _botService = botService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered BotAliveJob->Execute at: {time}", DateTimeOffset.Now);

            try
            {
                JobDataMap dataMap = context.JobDetail.JobDataMap;
                long? serverId = dataMap.GetLong("server_id");

                if (!serverId.HasValue)
                {
                    _logger.LogError("No value for variable serverId");
                    return;
                }

                await _botService.CheckBotState(serverId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
