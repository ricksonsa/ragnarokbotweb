using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Handlers
{
    public class ExclamationCommandHandlerFactory
    {
        private readonly Dictionary<string, IExclamationCommandHandler> _handlers;

        public ExclamationCommandHandlerFactory(
            IPlayerRepository playerRepository,
            IPlayerRegisterRepository playerRegisterRepository,
            IDiscordService discordService)
        {
            _handlers = new()
            {
               { "!welcomepack", new WelcomePackCommandHandler(playerRepository, playerRegisterRepository, discordService) },
               { "!discord", new DiscordCommandHandler() }
            };
        }

        public IExclamationCommandHandler? Create(string input)
        {
            if (input.StartsWith("!"))
            {
                string key = string.Empty;
                for (int i = 0; i < input.Length; i++)
                {
                    key += input[i];
                    if (_handlers.TryGetValue(key, out IExclamationCommandHandler? value)) return value;
                }
            }

            return null;
        }
    }
}
