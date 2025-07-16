using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class ListPlayersJob : IJob
    {
        private readonly ILogger<ListPlayersJob> _logger;
        private readonly ICacheService _cacheService;
        private readonly IBotService _botService;

        public ListPlayersJob(
            ICacheService cacheService,
            ILogger<ListPlayersJob> logger,
            IBotService botService)
        {
            _cacheService = cacheService;
            _logger = logger;
            _botService = botService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Triggered ListPlayersHostedService->Process at: {time}", DateTimeOffset.Now);

            try
            {
                JobDataMap dataMap = context.JobDetail.JobDataMap;
                long? serverId = dataMap.GetLong("server_id");
                if (!serverId.HasValue)
                {
                    _logger.LogError("No value for variable serverId");
                    return;
                }
                var bots = await _botService.FindActiveBotsByServerId(serverId.Value);

                foreach (var bot in bots)
                {
                    if (!_cacheService.GetCommandQueue(bot.ScumServer.Id).Any(command => command.Values.Any(cv => cv.Type == Shared.Enums.ECommandType.Delivery)))
                    {
                        var command = new BotCommand();
                        command.ListPlayers();
                        _cacheService.GetCommandQueue(bot.ScumServer.Id).Enqueue(command);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
