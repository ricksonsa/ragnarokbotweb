using Newtonsoft.Json;
using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
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
        private readonly IScumServerRepository _scumServerRepository;

        public BotService(
            IHttpContextAccessor httpContext,
            ICacheService cacheService,
            IPlayerService playerService,
            IOrderService orderService,
            ILogger<BotService> logger,
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork) : base(httpContext)
        {
            _cacheService = cacheService;
            _playerService = playerService;
            _orderService = orderService;
            _logger = logger;
            _scumServerRepository = scumServerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ConnectBot(Guid guid)
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            ValidateSubscription(server!);

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
            if (!serverId.HasValue)
                throw new UnauthorizedAccessException();

            _cacheService.GetConnectedBots(serverId.Value).TryRemove(guid, out var removedBot);
            _logger.LogInformation("Disconnecting Bot {Bot} from server {Server}", removedBot, serverId!.Value);
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

        public bool IsBotOnline()
        {
            var serverId = ServerId();

            return _cacheService.GetConnectedBots(serverId!.Value).Any();
        }

        public bool IsBotOnline(long serverId)
        {
            return _cacheService.GetConnectedBots(serverId).Any();
        }

        public async Task<BotCommand?> GetCommand(Guid guid)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            ValidateSubscription(server!);

            await RegisterBot(guid);

            if (_cacheService.TryDequeueCommand(serverId.Value, out var command))
            {
                if (command is not null && command.Values.Any(x => x.CheckTargetOnline))
                {
                    try
                    {
                        if (!(_cacheService.GetConnectedPlayers(serverId.Value).Any(player => player.SteamID == command.Values.FirstOrDefault(x => x.CheckTargetOnline)?.Target)))
                        {
                            _cacheService.EnqueueCommand(serverId.Value, command);
                            return null;
                        }
                    }
                    catch (Exception)
                    {
                        _logger.LogInformation("Retrieved commands {Command}", command.ToString());
                        return command;
                    }
                }
                _logger.LogInformation("Retrieved commands {Command}", command?.ToString());
                return command;
            }

            return null;
        }

        public BotCommand? PeekCommand(long serverId)
        {
            if (_cacheService.TryDequeueCommand(serverId, out var command))
            {
                if (command is not null) _cacheService.EnqueueCommand(serverId, command);
                return command;
            }
            return null;
        }

        public void PutCommand(BotCommand command)
        {
            var serverId = ServerId();
            if (!serverId.HasValue || AccessLevel() == Enums.AccessLevel.Default) throw new UnauthorizedAccessException();

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

        public List<BotUser> GetBots()
        {
            var serverId = ServerId();
            return _cacheService.GetConnectedBots(serverId!.Value).Values.ToList();
        }

        public async Task ResetBotState(long serverId)
        {
            var now = DateTime.UtcNow;
            var bots = _cacheService.GetConnectedBots(serverId).Values.ToList();
            foreach (var bot in bots)
            {
                var diff = (now - bot.LastPinged!.Value).TotalMinutes;
                if (diff >= 20)
                {
                    _cacheService.ClearConnectedPlayers(serverId);
                    DisconnectBot(bot.Guid);
                }
            }

            if (_cacheService.GetConnectedBots(serverId).Count == 0)
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
                if (_cacheService.GetConnectedBots(serverId.Value).TryGetValue(guid, out var bot)) return bot;
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task RegisterBot(Guid guid)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            ValidateSubscription(server!);

            _cacheService.GetConnectedBots(serverId.Value).AddOrUpdate(
                guid,
                _ =>
                {
                    var bot = new BotUser(guid);
                    _logger.LogInformation("Connecting bot {Bot} server {Server}", bot, serverId!.Value);
                    return bot;
                },
                (_, existing) =>
                {
                    existing.LastInteracted = DateTime.UtcNow;
                    _logger.LogInformation("Updating connected bot {Bot} server {Server}", existing, serverId!.Value);
                    return existing;
                }
            );
        }
    }
}
