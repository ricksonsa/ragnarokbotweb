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

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);
            try
            {
                JobDataMap dataMap = context.JobDetail.JobDataMap;
                long? serverId = dataMap.GetLong("server_id");
                if (!serverId.HasValue)
                {
                    _logger.LogError("No value for variable serverId");
                    return Task.CompletedTask;
                }

                _cacheService.ClearConnectedPlayers(serverId.Value);

                if (!_botService.IsBotOnline(serverId.Value)) return Task.CompletedTask;

                if (!_cacheService.GetCommandQueue(serverId.Value).Any(command => command.Values.Any(cv => cv.Type == Shared.Enums.ECommandType.SimpleDelivery)))
                {
                    var command = new BotCommand();
                    command.ListPlayers();
                    _cacheService.GetCommandQueue(serverId.Value).Enqueue(command);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.CompletedTask;
        }
    }
}
