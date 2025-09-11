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

        public async Task Execute(long serverId)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

            try
            {
                await _botService.ResetBotState(serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
