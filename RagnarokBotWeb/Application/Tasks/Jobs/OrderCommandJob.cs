using Microsoft.EntityFrameworkCore;
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

        public async Task Execute(long serverId)
        {
            var jobName = $"{GetType().Name}({serverId})";
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", jobName, DateTimeOffset.Now);

            try
            {
                var server = await GetServerAsync(serverId, ftpRequired: false, validateSubscription: true);

                var orders = await _orderRepository.FindManyByServerForCommand(server.Id);

                if (orders.Count == 0)
                {
                    _logger.LogDebug("{Job} -> No orders found, skipping execution", jobName);
                    return;
                }

                if (!_botService.IsBotOnline(server.Id))
                {
                    _logger.LogWarning("{Job} -> No bots online, skipping execution", jobName);
                    return;
                }

                var players = _cacheService.GetConnectedPlayers(server.Id);

                foreach (var notTrackedOrder in orders)
                {
                    var order = await _orderRepository.FindByIdAsync(notTrackedOrder.Id);

                    if (order?.Player?.SteamId64 is null)
                    {
                        _logger.LogDebug("{Job} -> Order {OrderId} has no player or SteamId, skipping", jobName, notTrackedOrder.Id);
                        continue;
                    }

                    var command = new BotCommand();
                    switch (order.OrderType)
                    {
                        case EOrderType.Pack:
                            await HandlePackOrder(order, command);
                            break;
                        case EOrderType.Warzone:
                            await HandleWarzoneOrder(order, command);
                            break;
                        case EOrderType.UAV:
                            await HandleUavOrder(server, players, order, command);
                            break;
                        case EOrderType.Taxi:
                            await HandleTaxiOrder(order, command);
                            break;
                        case EOrderType.Exchange:
                            await HandleExchangeOrder(_unitOfWork, order, command);
                            break;
                    }

                    if (order.OrderType != EOrderType.UAV)
                    {
                        await _unitOfWork.AppDbContext.Database.ExecuteSqlAsync($@"UPDATE ""Orders"" SET ""Status"" = {(int)EOrderStatus.Command} WHERE ""Id"" = {order.Id}");
                    }
                }
            }
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Job} Exception -> {Message}", jobName, ex.Message);
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

            await _botService.SendCommand(order.ScumServer.Id, command);
            await _unitOfWork.AppDbContext.Database.ExecuteSqlAsync($@"UPDATE ""Orders"" SET ""Status"" = {(int)EOrderStatus.Done} WHERE ""Id"" = {order.Id}");
        }

        private async Task HandleWarzoneOrder(Order order, BotCommand command)
        {
            if (order.Warzone is null) return;
            var teleport = WarzoneRandomSelector.SelectTeleportPoint(order.Warzone!);
            command.Data = "order_" + order.Id.ToString();
            command.Teleport(order.Player!.SteamId64!, teleport.Teleport.Coordinates);
            await _botService.SendCommand(order.ScumServer.Id, command);
        }

        private async Task HandleTaxiOrder(Order order, BotCommand command)
        {
            if (order.Taxi is null) return;
            var teleport = order.Taxi.TaxiTeleports.FirstOrDefault(t => t.Id.ToString() == order.TaxiTeleportId);
            if (teleport is null) return;
            command.Data = "order_" + order.Id.ToString();
            command.Teleport(order.Player!.SteamId64!, teleport.Teleport.Coordinates);
            await _botService.SendCommand(order.ScumServer.Id, command);
        }

        private async Task HandleExchangeOrder(IUnitOfWork uow, Order order, BotCommand command)
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
            await _botService.SendCommand(order.ScumServer.Id, command);
        }

        private async Task HandlePackOrder(Order order, BotCommand command)
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
            await _botService.SendCommand(order.ScumServer.Id, command);
        }
    }
}
