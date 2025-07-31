using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Parser;

namespace RagnarokBotWeb.Domain.Services
{
    public class BotService : BaseService, IBotService
    {
        private readonly ILogger<BotService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IPlayerService _playerService;
        private readonly IOrderService _orderService;

        public BotService(
            IHttpContextAccessor httpContext,
            ICacheService cacheService,
            IPlayerService playerService,
            IOrderService orderService,
            ILogger<BotService> logger) : base(httpContext)
        {
            _cacheService = cacheService;
            _playerService = playerService;
            _orderService = orderService;
            _logger = logger;
        }

        public void ConnectBot(Guid guid)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            try
            {
                if (_cacheService.GetConnectedBots(serverId.Value).TryGetValue(guid, out var bot))
                {
                    bot.LastInteracted = DateTime.UtcNow;
                    _cacheService.GetConnectedBots(serverId.Value)[guid] = bot;
                }
                else
                {
                    _cacheService.GetConnectedBots(serverId.Value)[guid] = new BotUser(guid);
                }

            }
            catch (Exception) { }
        }

        public void DisconnectBot(Guid guid)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            _cacheService.GetConnectedBots(serverId.Value).Remove(guid);
        }

        public async Task UpdatePlayersOnline(PlayersListRequest input)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            var players = ListPlayersParser.ParsePlayers(input.Value);
            _cacheService.SetConnectedPlayers(serverId.Value, players);
            await _playerService.UpdateFromScumPlayers(serverId.Value, players);
        }

        public bool IsBotOnline()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            return _cacheService.GetConnectedBots(serverId.Value).Any();
        }

        public bool IsBotOnline(long serverId)
        {
            return _cacheService.GetConnectedBots(serverId).Any();
        }

        public BotCommand? GetCommand(Guid guid)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            ConnectBot(guid);

            if (_cacheService.GetCommandQueue(serverId.Value).TryDequeue(out var command))
            {
                return command;
            }

            return null;
        }

        public void PutCommand(BotCommand command)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            _cacheService.GetCommandQueue(serverId.Value).Enqueue(command);
        }

        public async Task ConfirmDelivery(long orderId)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();
            await _orderService.ConfirmOrderDelivered(orderId);
        }

        public List<BotUser> FindActiveBotsByServerId(long serverId)
        {
            return _cacheService.GetConnectedBots(serverId).Values.ToList();
        }

        public void ResetBotState(long serverId)
        {
            var now = DateTime.UtcNow;
            var bots = _cacheService.GetConnectedBots(serverId).Values.ToList();
            foreach (var bot in bots)
            {
                TimeSpan difference = now - bot.LastInteracted;
                if (difference.Minutes >= 2)
                {
                    _cacheService.GetConnectedBots(serverId).Remove(bot.Guid);
                }
            }
        }

        public BotUser? FindBotByGuid(Guid guid)
        {

            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            try
            {
                return _cacheService.GetConnectedBots(serverId.Value)[guid];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void RegisterBot(Guid guid)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            try
            {
                if (_cacheService.GetConnectedBots(serverId.Value).TryGetValue(guid, out var bot))
                {
                    bot.LastInteracted = DateTime.UtcNow;
                    bot.LastPinged = DateTime.UtcNow;
                    _cacheService.GetConnectedBots(serverId.Value)[guid] = bot;
                }
                else
                {
                    _cacheService.GetConnectedBots(serverId.Value)[guid] = new BotUser(guid);
                }

            }
            catch (Exception) { }
        }
    }
}
