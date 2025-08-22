using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class CustomJob : IJob
    {
        private ILogger<CustomJob> _logger;
        private readonly ICacheService _cacheService;
        private readonly IBotService _botService;

        public CustomJob(ILogger<CustomJob> logger, ICacheService cacheService)
        {
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobName = context.JobDetail.Key.Name;

            JobDataMap dataMap = context.JobDetail.JobDataMap;
            long? serverId = dataMap.GetLong("server_id");
            string? commandsString = dataMap.GetString("commands");

            if (serverId is null) return;
            if (commandsString is null) return;

            IEnumerable<string> commands = commandsString.ToString()!.Split(";");

            _logger.LogDebug($"Executing job: {jobName} from serverId: {serverId.Value} at {DateTime.Now}");


            foreach (var command in commands)
            {
                var botCommand = new BotCommand();
                botCommand.Command(command);
                await _botService.SendCommand(serverId.Value, botCommand);
            }
        }
    }
}
