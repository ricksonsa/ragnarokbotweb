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

            var serverIdObject = context.Get("server_id");
            if (serverIdObject is null) return Task.CompletedTask;

            var commandsObject = context.Get("commands");
            if (commandsObject is null) return Task.CompletedTask;

            IEnumerable<string> commands = commandsObject.ToString()!.Split(";");

            _logger.LogInformation($"Executing job: {jobName} from serverId: {serverIdObject} at {DateTime.Now}");

            if (long.TryParse(serverIdObject.ToString(), out long serverId))
            {
                foreach (var command in commands)
                {
                    _cacheService.GetCommandQueue(serverId).Enqueue(BotCommand.Command(command));
                }
            }

            return Task.CompletedTask;
        }
    }
}
