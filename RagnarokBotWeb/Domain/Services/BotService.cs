using Newtonsoft.Json;
using RagnarokBotWeb.Application.BotServer;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;
using Shared.Parser;

namespace RagnarokBotWeb.Domain.Services
{
    public class BotService : BaseService, IBotService
    {
        private readonly ILogger<BotService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IPlayerService _playerService;
        private readonly IOrderService _orderService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BotSocketServer _botSocket;
        private readonly IScumServerRepository _scumServerRepository;

        public BotService(
            IHttpContextAccessor httpContext,
            ICacheService cacheService,
            IPlayerService playerService,
            IOrderService orderService,
            ILogger<BotService> logger,
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            BotSocketServer botSocket) : base(httpContext)
        {
            _cacheService = cacheService;
            _playerService = playerService;
            _orderService = orderService;
            _logger = logger;
            _scumServerRepository = scumServerRepository;
            _unitOfWork = unitOfWork;
            _botSocket = botSocket;
        }

        public async Task UpdatePlayersOnline(UpdateFromStringRequest input)
        {
            var serverId = ServerId();
            var players = ListPlayersParser.Parse(input.Value);
            _cacheService.SetConnectedPlayers(serverId!.Value, players);
            await _playerService.UpdateFromScumPlayers(serverId.Value, players);
        }

        public async Task UpdateFlags(UpdateFromStringRequest input)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) return;

            var flags = ListFlagsParser.Parse(input.Value);
            _cacheService.SetFlags(serverId.Value, flags);
            await new ScumFileProcessor(server, _unitOfWork).SaveFlagList(JsonConvert.SerializeObject(flags, Formatting.Indented));
        }

        public async Task UpdateSquads(UpdateFromStringRequest input)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) return;

            var squads = ListSquadsParser.Parse(input.Value);
            _cacheService.SetSquads(serverId.Value, squads);
            await new ScumFileProcessor(server, _unitOfWork).SaveSquadList(JsonConvert.SerializeObject(squads, Formatting.Indented));
        }

        public async Task SendCommand(long serverId, BotCommand command)
        {
            await _botSocket.SendCommandAsync(serverId, command);
        }

        public bool IsBotOnline()
        {
            var serverId = ServerId();
            return _botSocket.IsBotConnected(serverId!.Value);
        }

        public bool IsBotOnline(long serverId)
        {
            return _botSocket.IsBotConnected(serverId);
        }

        public async Task ConfirmDelivery(long orderId)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();
            await _orderService.ConfirmOrderDelivered(orderId);
        }

        public List<BotUser> FindActiveBotsByServerId(long serverId)
        {
            return _botSocket.GetBots(serverId).Where(bot => bot.LastPinged.HasValue).ToList();
        }

        public List<BotUser> GetBots()
        {
            var serverId = ServerId();
            return _botSocket.GetBots(serverId!.Value);
        }

        public List<BotUser> GetConnectedBots()
        {
            var serverId = ServerId();
            return _botSocket.GetBots(serverId!.Value).Where(bot => bot.LastPinged.HasValue).ToList();
        }

        public async Task ResetBotState(long serverId)
        {
            var now = DateTime.UtcNow;
            var bots = _botSocket.GetBots(serverId).Where(bot => bot.LastPinged.HasValue);
            foreach (var bot in bots)
            {
                var diff = (now - bot.LastPinged!.Value).TotalMinutes;
                if (diff >= 5)
                    await _botSocket.SendCommandAsync(serverId, bot.Guid.ToString(), new BotCommand().Reconnect());
            }

            if (bots.Count() == 0)
            {
                await _orderService.ResetCommandOrders(serverId);
            }
        }

        public async Task<BotUser?> FindBotByGuid(Guid guid)
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);

            ValidateSubscription(server!);

            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            try
            {
                return _botSocket.GetBots(serverId.Value).FirstOrDefault(bot => bot.Guid == guid);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
