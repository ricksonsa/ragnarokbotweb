using RagnarokBotWeb.Application.Resolvers;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Handlers
{
    public class ExclamationCommandHandlerFactory
    {
        private readonly Dictionary<string, IExclamationCommandHandler> _handlers;

        public ExclamationCommandHandlerFactory(
            ScumServer server,
            IBotService botService,
            IScumServerRepository scumServerRepository,
            IPlayerRepository playerRepository,
            IPlayerRegisterRepository playerRegisterRepository,
            IDiscordService discordService,
            IOrderService orderService,
            SteamAccountResolver steamAccountResolver)
        {
            _handlers = new()
            {
               { "!welcomepack", new WelcomePackCommandHandler(playerRepository, playerRegisterRepository, discordService, orderService, steamAccountResolver) },
               { "!discord", new DiscordCommandHandler(server, scumServerRepository, botService) },
               { "!orderconfirm", new ConfirmOrderCommand(orderService) }
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
