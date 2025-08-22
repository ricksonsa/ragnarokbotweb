using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Handlers
{
    public class DiscordCommandHandler : IExclamationCommandHandler
    {
        private readonly ScumServer _server;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IBotService _botService;

        public DiscordCommandHandler(
            ScumServer server,
            IScumServerRepository scumServerRepository,
            IBotService botService)
        {
            _server = server;
            _scumServerRepository = scumServerRepository;
            _botService = botService;
        }

        public async Task ExecuteAsync(ChatTextParseResult value)
        {
            var server = await _scumServerRepository.FindActiveById(_server.Id);
            if (server == null || server.Guild == null) return;

            if (!string.IsNullOrEmpty(server.Guild.DiscordLink))
            {
                await _botService.SendCommand(server.Id, new Shared.Models.BotCommand().Say(server.Guild.DiscordLink));
                value.Post = false;
            }
        }
    }
}
