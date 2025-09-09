using Quartz;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Domain.Business;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;
using Shared.Models;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class OrderCommandJob : AbstractJob, IJob
    {
        private readonly ILogger<OrderCommandJob> _logger;
        private readonly ICacheService _cacheService;
        private readonly IOrderRepository _orderRepository;
        private readonly IDiscordService _discordService;
        private readonly IFileService _fileService;
        private readonly IBotService _botService;
        private readonly IUnitOfWork _unitOfWork;

        public OrderCommandJob(
            ICacheService cacheService,
            IScumServerRepository scumServerRepository,
            IBotService botService,
            IOrderRepository orderRepository,
            ILogger<OrderCommandJob> logger,
            IDiscordService discordService,
            IFileService fileService,
            IUnitOfWork unitOfWork) : base(scumServerRepository)
        {
            _cacheService = cacheService;
            _botService = botService;
            _orderRepository = orderRepository;
            _logger = logger;
            _discordService = discordService;
            _fileService = fileService;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            try
            {
                var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);
                var orders = await _orderRepository.FindManyByServerForCommand(server.Id);

                if (orders.Count == 0)
                {
                    _logger.LogInformation("{Job} -> No orders found, skipping execution", context.JobDetail.Key.Name);
                    return;
                }

                if (!_botService.IsBotOnline(server.Id))
                {
                    _logger.LogInformation("{Job} -> No bots online, skipping execution", context.JobDetail.Key.Name);
                    return;
                }

                var players = _cacheService.GetConnectedPlayers(server.Id);
                foreach (var order in orders)
                {
                    if (order.Player?.SteamId64 is null)
                    {
                        _logger.LogInformation("{Job} -> Order have no player or player steamId is null, skipping execution", context.JobDetail.Key.Name);
                        return;
                    }

                    order.Status = EOrderStatus.Command;
                    _orderRepository.Update(order);
                    await _orderRepository.SaveAsync();
                    var command = new BotCommand();

                    switch (order.OrderType)
                    {
                        case EOrderType.Pack:
                            HandlePackOrder(order, command);
                            break;
                        case EOrderType.Warzone:
                            HandleWarzoneOrder(order, command);
                            break;
                        case EOrderType.UAV:
                            await HandleUavOrder(server, players, order, command);
                            break;
                        case EOrderType.Taxi:
                            HandleTaxiOrder(order, command);
                            break;
                        case EOrderType.Exchange:
                            await HandleExchangeOrder(_unitOfWork, order, command);
                            break;
                    }

                    if (command != null) await _botService.SendCommand(order.ScumServer.Id, command);

                }
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                _logger.LogError("OrderCommandJob Exception -> {Ex}", ex.Message);
                throw;
            }
        }

        private async Task HandleUavOrder(Domain.Entities.ScumServer server, List<ScumPlayer> players, Order order, BotCommand command)
        {
            if (order.ScumServer?.Uav is null) return;

            await new UavHandler(
                _discordService,
                _fileService,
                players,
                server).Execute(order.Player!, order.Uav!);

            var deliveryText = order.ResolvedDeliveryText();
            if (deliveryText is not null) command.Say(deliveryText);

            order.Status = EOrderStatus.Done;
            _orderRepository.Update(order);
            await _orderRepository.SaveAsync();
        }

        private static void HandleWarzoneOrder(Order order, BotCommand command)
        {
            if (order.Warzone is null) return;
            var teleport = WarzoneRandomSelector.SelectTeleportPoint(order.Warzone!);
            command.Data = "order_" + order.Id.ToString();
            command.Teleport(order.Player!.SteamId64!, teleport.Teleport.Coordinates);
        }

        private static void HandleTaxiOrder(Order order, BotCommand command)
        {
            if (order.Taxi is null) return;
            var teleport = order.Taxi.TaxiTeleports.FirstOrDefault(t => t.Id.ToString() == order.TaxiTeleportId);
            if (teleport is null) return;
            command.Data = "order_" + order.Id.ToString();
            command.Teleport(order.Player!.SteamId64!, teleport.Teleport.Coordinates);
        }

        private static async Task HandleExchangeOrder(IUnitOfWork uow, Order order, BotCommand command)
        {
            if (order.Player is null) return;
            if (order.Player.ScumServer.Exchange is null) return;
            command.Data = "order_" + order.Id.ToString();

            var converter = new CoinConverterManager(order.ScumServer);
            var coinManager = new PlayerCoinManager(uow);
            switch (order.ExchangeType)
            {
                case EExchangeType.Withdraw:
                    var withdraw = converter.ToInGameMoney(order.ExchangeAmount);
                    if (order.Player.Coin < order.ExchangeAmount)
                    {
                        order.Status = EOrderStatus.Canceled;
                        return;
                    }
                    await coinManager.RemoveCoinsByPlayerId(order.Player.Id, order.ExchangeAmount);
                    if (order.ScumServer.Exchange.CurrencyType == Domain.Enums.EExchangeGameCurrencyType.Money)
                        command.ChangeMoney(order.Player!.SteamId64!, withdraw);
                    else if (order.ScumServer.Exchange.CurrencyType == Domain.Enums.EExchangeGameCurrencyType.Gold)
                        command.ChangeGold(order.Player!.SteamId64!, withdraw);
                    break;

                case EExchangeType.Deposit:
                    var deposit = converter.ToDiscordCoins(order.ExchangeAmount);
                    if (!order.Player.HasBalance(order.ExchangeAmount, order.ScumServer.Exchange.CurrencyType))
                    {
                        order.Status = EOrderStatus.Canceled;
                        return;
                    }
                    await coinManager.AddCoinsByPlayerId(order.Player.Id, deposit);
                    if (order.ScumServer.Exchange.CurrencyType == Domain.Enums.EExchangeGameCurrencyType.Money)
                        command.ChangeMoney(order.Player!.SteamId64!, -order.ExchangeAmount);
                    else if (order.ScumServer.Exchange.CurrencyType == Domain.Enums.EExchangeGameCurrencyType.Gold)
                        command.ChangeGold(order.Player!.SteamId64!, -order.ExchangeAmount);
                    break;
            }
        }

        private static void HandlePackOrder(Order order, BotCommand command)
        {
            if (order.Pack is null) return;

            var deliveryText = order.ResolvedDeliveryText();
            if (deliveryText is not null) command.Say(deliveryText);

            foreach (var packItem in order.Pack.PackItems)
            {
                if (packItem.AmmoCount > 0)
                    command.MagazineDelivery(order.Player!.SteamId64!, packItem.Item.Code, packItem.Amount, packItem.AmmoCount);
                else
                    command.Delivery(order.Player!.SteamId64!, packItem.Item.Code, packItem.Amount);
            }

            command.Data = "order_" + order.Id.ToString();
        }
    }
}
