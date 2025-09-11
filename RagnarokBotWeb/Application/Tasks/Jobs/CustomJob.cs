using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class CustomJob : ICustomJob
    {
        private ILogger<CustomJob> _logger;
        private readonly IBotService _botService;

        public CustomJob(ILogger<CustomJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(long serverId, string commandString)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);
            if (commandString is null) return;

            IEnumerable<string> commands = commandString.ToString()!.Split(";");

            foreach (var command in commands)
            {
                var botCommand = new BotCommand();
                botCommand.Command(command);
                await _botService.SendCommand(serverId, botCommand);
            }
        }
    }
}
