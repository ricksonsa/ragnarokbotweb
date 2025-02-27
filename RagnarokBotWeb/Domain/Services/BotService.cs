using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;
using Shared.Parser;

namespace RagnarokBotWeb.Domain.Services
{
    public class BotService : IBotService
    {
        private readonly IBotRepository _botRepository;
        private readonly ICacheService _cacheService;
        private readonly IPlayerService _playerService;

        public BotService(IBotRepository botRepository, ICacheService cacheService, IPlayerService playerService)
        {
            _botRepository = botRepository;
            _cacheService = cacheService;
            _playerService = playerService;
        }

        public async Task<Bot> RegisterBot(string identifier)
        {
            var bot = await _botRepository.FindOneAsync(b => b.Identifier == identifier);
            bot ??= new();
            bot.State = EBotState.Online;
            bot.Active = true;
            bot.Identifier = identifier;
            _botRepository.Update(bot);
            await _botRepository.SaveAsync();
            return bot;
        }

        public async Task UpdatePlayersOnline(string input, string identifier)
        {
            await UpdateInteraction(identifier);
            var players = ListPlayersParser.ParsePlayers(input);
            _cacheService.ClearConnectedPlayers();
            _cacheService.SetConnectedPlayers(players);
            await _playerService.UpdateFromScumPlayers(players);
        }

        public async Task UpdateInteraction(string identifier)
        {
            var bot = await RegisterBot(identifier);
            bot.UpdateInteraction();
            _botRepository.Update(bot);
            await _botRepository.SaveAsync();
        }

        public async Task<Bot?> UnregisterBot(string identifier)
        {
            var bot = await _botRepository.FindOneAsync(b => b.Identifier == identifier);
            if (bot is null) return null;
            bot.State = EBotState.Offline;
            _botRepository.Update(bot);
            await _botRepository.SaveAsync();
            return bot;
        }

        public async Task CheckBotState()
        {
            var date = DateTime.Now.AddMinutes(-2);
            var bots = await _botRepository.FindAsync(bot => bot.State == EBotState.Online && bot.LastInteracted <= date);
            foreach (var bot in bots)
            {
                bot.State = EBotState.Offline;
                _botRepository.Update(bot);
            }
            await _botRepository.SaveAsync();
        }
    }
}
