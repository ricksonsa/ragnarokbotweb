using RagnarokBotWeb.Application;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;
using Shared.Parser;

namespace RagnarokBotWeb.Domain.Services
{
    public class BotService : BaseService, IBotService
    {
        private readonly IBotRepository _botRepository;
        private readonly ICacheService _cacheService;
        private readonly IPlayerService _playerService;

        public BotService(
            IHttpContextAccessor httpContext,
            IBotRepository botRepository,
            ICacheService cacheService,
            IPlayerService playerService) : base(httpContext)
        {
            _botRepository = botRepository;
            _cacheService = cacheService;
            _playerService = playerService;
        }

        public async Task<Bot> RegisterBot()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            var bot = await _botRepository.FindByScumServerId(serverId.Value);
            if (bot is null) throw new DomainException($"No active bot found for server [{serverId.Value}]");

            bot.State = EBotState.Online;
            bot.Active = true;
            _botRepository.Update(bot);
            await _botRepository.SaveAsync();
            return bot;
        }

        public async Task UpdatePlayersOnline(string input)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            await UpdateInteraction();
            var players = ListPlayersParser.ParsePlayers(input);
            _cacheService.ClearConnectedPlayers(serverId.Value);
            _cacheService.SetConnectedPlayers(serverId.Value, players);
            await _playerService.UpdateFromScumPlayers(serverId.Value, players);
        }

        public async Task UpdateInteraction()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            var bot = await _botRepository.FindByScumServerId(serverId.Value);
            if (bot is null) throw new DomainException($"No active bot found for server [{serverId.Value}]");

            bot.UpdateInteraction();
            _botRepository.Update(bot);
            await _botRepository.SaveAsync();
        }

        public async Task<Bot?> UnregisterBot()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            var bot = await _botRepository.FindByScumServerId(serverId.Value);
            if (bot is null) throw new DomainException($"No active bot found for server [{serverId.Value}]");

            bot.State = EBotState.Offline;
            _botRepository.Update(bot);
            await _botRepository.SaveAsync();
            return bot;
        }

        public async Task CheckBotState(long serverId)
        {
            var bots = await _botRepository.FindByServerIdOnlineAndLastInteraction(serverId);
            foreach (var bot in bots)
            {
                bot.State = EBotState.Offline;
                _botRepository.Update(bot);
            }
            await _botRepository.SaveAsync();
        }

        public async Task<BotCommand?> GetCommand()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            await UpdateInteraction();
            if (_cacheService.GetCommandQueue(serverId.Value).TryDequeue(out var command))
            {
                return command;
            }

            return null;
        }

        public async Task PutCommand(BotCommand command)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            await UpdateInteraction();
            _cacheService.GetCommandQueue(serverId.Value).Enqueue(command);
        }

        public Task<List<Bot>> FindActiveBotsByServerId(long serverId)
        {
            return _botRepository.FindActiveBotsByServerId(serverId);
        }
    }
}
