using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class CustomJob : IJob
    {
        private ILogger<CustomJob> _logger;
        private readonly ICacheService _cacheService;

        public CustomJob(ILogger<CustomJob> logger, ICacheService cacheService)
        {
            _logger = logger;
            _cacheService = cacheService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            var jobName = context.JobDetail.Key.Name;

            JobDataMap dataMap = context.JobDetail.JobDataMap;
            long? serverId = dataMap.GetLong("server_id");
            string? commandsString = dataMap.GetString("commands");

            if (serverId is null) return Task.CompletedTask;
            if (commandsString is null) return Task.CompletedTask;

            IEnumerable<string> commands = commandsString.ToString()!.Split(";");

            _logger.LogInformation($"Executing job: {jobName} from serverId: {serverId.Value} at {DateTime.Now}");


            foreach (var command in commands)
            {
                _cacheService.GetCommandQueue(serverId.Value).Enqueue(BotCommand.Command(command));
            }

            return Task.CompletedTask;
        }
    }
}
