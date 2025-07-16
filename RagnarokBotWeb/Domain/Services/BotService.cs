using AutoMapper;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;
using Shared.Parser;

namespace RagnarokBotWeb.Domain.Services
{
    public class BotService : BaseService, IBotService
    {
        private readonly IBotRepository _botRepository;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ICacheService _cacheService;
        private readonly IPlayerService _playerService;
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public BotService(
            IHttpContextAccessor httpContext,
            IBotRepository botRepository,
            ICacheService cacheService,
            IPlayerService playerService,
            IScumServerRepository scumServerRepository,
            IMapper mapper,
            IOrderService orderService) : base(httpContext)
        {
            _botRepository = botRepository;
            _cacheService = cacheService;
            _playerService = playerService;
            _scumServerRepository = scumServerRepository;
            _mapper = mapper;
            _orderService = orderService;
        }

        public async Task<BotDto> RegisterBot()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            var bot = await _botRepository.FindByScumServerId(serverId.Value);
            //if (bot is null) throw new DomainException($"No active bot found for server [{serverId.Value}]");

            if (bot is null)
            {
                bot = new Bot
                {
                    ScumServer = await _scumServerRepository.FindActiveById(serverId.Value),
                };
                await _botRepository.CreateOrUpdateAsync(bot);
                await _botRepository.SaveAsync();
            }

            bot.State = EBotState.Online;
            bot.Active = true;
            bot.LastInteracted = DateTime.UtcNow;
            _botRepository.Update(bot);
            await _botRepository.SaveAsync();
            return _mapper.Map<BotDto>(bot);
        }

        public async Task UpdatePlayersOnline(PlayersListRequest input)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            var players = ListPlayersParser.ParsePlayers(input.Value);
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

        public async Task<BotDto?> UnregisterBot()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            var bot = await _botRepository.FindByScumServerId(serverId.Value);
            if (bot is null) throw new DomainException($"No active bot found for server [{serverId.Value}]");

            bot.State = EBotState.Offline;
            _botRepository.Update(bot);
            await _botRepository.SaveAsync();
            return _mapper.Map<BotDto>(bot);
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

        public async Task CheckBotState(ulong guildId)
        {
            var bots = await _botRepository.FindByServerIdOnlineAndLastInteraction(guildId);
            foreach (var bot in bots)
            {
                bot.State = EBotState.Offline;
                _botRepository.Update(bot);
            }
            await _botRepository.SaveAsync();
        }

        public async Task<bool> IsBotOnline(ulong guildId)
        {
            var bots = await _botRepository.FindOnlineBotByGuild(guildId);
            return bots.Any();
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

        public async Task ConfirmDelivery(long orderId)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            await UpdateInteraction();
            await _orderService.ConfirmOrderDelivered(orderId);
        }
    }
}
