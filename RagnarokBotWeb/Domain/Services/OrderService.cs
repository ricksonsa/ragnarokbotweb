using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Crosscutting.Utils;
using RagnarokBotWeb.Domain.Business;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class OrderService : BaseService, IOrderService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly IOrderRepository _orderRepository;
        private readonly IPackRepository _packRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IWarzoneRepository _warzoneRepository;
        private readonly ITaxiRepository _taxiRepository;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly IBotService _botService;
        private readonly IFileService _fileService;
        private readonly IDiscordService _discordService;

        public OrderService(
            IHttpContextAccessor contextAccessor,
            ILogger<OrderService> logger,
            IOrderRepository orderRepository,
            IPackRepository packRepository,
            IPlayerRepository userRepository,
            IMapper mapper,
            IWarzoneRepository warzoneRepository,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IScumServerRepository scumServerRepository,
            ITaxiRepository taxiRepository,
            IBotService botService,
            IFileService fileService,
            IDiscordService discordService) : base(contextAccessor)
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _packRepository = packRepository;
            _playerRepository = userRepository;
            _mapper = mapper;
            _warzoneRepository = warzoneRepository;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _scumServerRepository = scumServerRepository;
            _taxiRepository = taxiRepository;
            _botService = botService;
            _fileService = fileService;
            _discordService = discordService;
        }

        public async Task ConfirmServerDelivery(long orderId)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedAccessException();
            await ConfirmOrderDelivered(orderId);
        }

        public async Task<OrderDto> ConfirmOrderDelivered(long orderId)
        {
            var order = await _orderRepository.FindByIdAsync(orderId);
            if (order is null) throw new NotFoundException("Order not found");

            order.Status = EOrderStatus.Done;
            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();
            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto> CancelOrder(long orderId)
        {
            var order = await _orderRepository.FindByIdAsync(orderId);
            if (order is null) throw new NotFoundException("Order not found");

            order.Status = EOrderStatus.Canceled;
            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();

            if (order.Player != null)
            {
                var price = order.GetPrice();
                var manager = new PlayerCoinManager(_unitOfWork);
                await manager.AddCoinsByPlayerId(order.Player.Id, price);
            }

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto> RequeueOrder(long orderId)
        {
            var order = await _orderRepository.FindByIdAsync(orderId);
            if (order is null) throw new NotFoundException("Order not found");

            order.Status = EOrderStatus.Created;
            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();
            return _mapper.Map<OrderDto>(order);
        }

        public Task<IEnumerable<Order>> GetCreatedOrders()
        {
            return _orderRepository.FindAsync(order => order.Status == EOrderStatus.Created);
        }

        public async Task<Page<OrderDto>> GetPacksPageByFilterAsync(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            Page<Order> page = await _orderRepository.GetPageByFilter(serverId!.Value, paginator, filter);
            return new Page<OrderDto>(page.Content.Select(_mapper.Map<OrderDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public async Task<OrderDto?> PlaceWelcomePackOrder(long playerId)
        {
            var player = await _playerRepository.FindByIdAsync(playerId);
            if (player is null) throw new NotFoundException("Player not found");

            if (!_cacheService.GetConnectedPlayers(player.ScumServerId).Any(p => p.SteamID == player.SteamId64))
                throw new NotFoundException($"Player {player.Name} is not online");

            return _mapper.Map<OrderDto>(await PlaceWelcomePackOrder(player));
        }

        public async Task<Order?> PlaceWelcomePackOrder(Player player)
        {
            var pack = await _packRepository.FindWelcomePackByServerIdAsync(player.ScumServer.Id);
            if (pack is null) return null;

            var order = new Order
            {
                Pack = pack,
                Player = player,
                OrderType = EOrderType.Pack,
                ScumServer = player.ScumServer
            };

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();

            return order;
        }

        public async Task<Order?> PlaceDeliveryOrder(string identifier, long packId)
        {
            var pack = await _packRepository.FindByIdAsync(packId);
            if (pack is null) throw new DomainException($"Pack [{packId}] not found");

            var player = await _playerRepository.FindOneWithServerAsync(u => u.SteamId64 == identifier || u.DiscordId.ToString() == identifier);
            if (player is null) throw new NotFoundException("Player not found");

            var order = new Order
            {
                Pack = pack,
                Player = player,
                OrderType = EOrderType.Pack,
                ScumServer = player.ScumServer
            };

            var processor = new OrderPurchaseProcessor(_orderRepository, _cacheService);
            await processor.ValidateAsync(order);
            var price = order.ResolvedPrice;

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();
            await new PlayerCoinManager(_unitOfWork).RemoveCoinsByPlayerId(player.Id, price);
            return order;
        }

        public async Task<Order?> PlaceDeliveryOrderFromDiscord(ulong guildId, ulong discordId, long packId)
        {
            var pack = await _packRepository.FindByIdAsync(packId);
            if (pack is null) throw new DomainException($"Pack [{packId}] not found");

            var player = await _playerRepository.FindOneWithServerAsync(u => u.ScumServer.Guild != null && u.ScumServer.Guild.DiscordId == guildId && u.DiscordId == discordId);
            if (player is null) throw new DomainException("You are not registered, please register using the Welcome Pack.");

            var order = new Order
            {
                Pack = pack,
                Player = player,
                OrderType = EOrderType.Pack,
                ScumServer = player.ScumServer
            };

            var processor = new OrderPurchaseProcessor(_orderRepository, _cacheService);
            await processor.ValidateAsync(order);
            var price = order.ResolvedPrice;

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();
            await new PlayerCoinManager(_unitOfWork).RemoveCoinsByPlayerId(player.Id, price);

            return order;
        }

        public async Task<Order?> PlaceUavOrderFromDiscord(Entities.ScumServer server, ulong userDiscordId, string sector)
        {
            var player = await _playerRepository.FindOneWithServerAsync(u => u.ScumServer.Id == server.Id && u.DiscordId == userDiscordId);
            if (player is null) throw new DomainException("You are not registered, please register using the Welcome Pack.");

            var order = new Order
            {
                Uav = sector,
                Player = player,
                OrderType = EOrderType.UAV,
                ScumServer = player.ScumServer
            };

            var processor = new OrderPurchaseProcessor(_orderRepository, _cacheService);
            await processor.ValidateAsync(order);
            var price = order.ResolvedPrice;

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();
            await new PlayerCoinManager(_unitOfWork).RemoveCoinsByPlayerId(player.Id, price);

            return order;
        }

        public async Task CreateOrder(Order order)
        {
            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();
        }

        public async Task ProcessOrder(Order order)
        {
            try
            {
                if (!_botService.IsBotOnline(order.ScumServer.Id)) return;
                var players = _cacheService.GetConnectedPlayers(order.ScumServer.Id);
                if (!players.Any(p => p.SteamID == order.Player!.SteamId64)) return;
                if (order?.Player?.SteamId64 is null) return;

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
                        await HandleUavOrder(players, order, command);
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
            catch (ServerUncompliantException) { }
            catch (FtpNotSetException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessOrder Exception");
            }
        }

        private async Task HandleUavOrder(List<ScumPlayer> players, Order order, BotCommand command)
        {
            if (order.ScumServer?.Uav is null) return;

            await new UavHandler(
                _discordService,
                _fileService,
                players,
                order.ScumServer).Execute(order.Player!, order.Uav!);

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
                    if (order.ScumServer.Exchange.CurrencyType == Enums.EExchangeGameCurrencyType.Money)
                        command.ChangeMoney(order.Player!.SteamId64!, withdraw);
                    else if (order.ScumServer.Exchange.CurrencyType == Enums.EExchangeGameCurrencyType.Gold)
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
                    if (order.ScumServer.Exchange.CurrencyType == Enums.EExchangeGameCurrencyType.Money)
                        command.ChangeMoney(order.Player!.SteamId64!, -order.ExchangeAmount);
                    else if (order.ScumServer.Exchange.CurrencyType == Enums.EExchangeGameCurrencyType.Gold)
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

        public async Task<Order?> PlaceWarzoneOrderFromDiscord(ulong guildId, ulong discordId, long warzoneId)
        {
            var warzone = await _warzoneRepository.FindByIdAsync(warzoneId);
            if (warzone is null) throw new DomainException("Warzone not found");

            var player = await _playerRepository.FindOneWithServerAsync(u => u.ScumServer.Guild != null && u.ScumServer.Guild.DiscordId == guildId && u.DiscordId == discordId);
            if (player is null) throw new DomainException("You are not registered, please register using the Welcome Pack.");

            var order = new Order
            {
                Warzone = warzone,
                Player = player,
                OrderType = EOrderType.Warzone,
                ScumServer = player.ScumServer
            };

            var processor = new OrderPurchaseProcessor(_orderRepository, _cacheService);
            await processor.ValidateAsync(order);
            var price = order.ResolvedPrice;

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();
            await new PlayerCoinManager(_unitOfWork).RemoveCoinsByPlayerId(player.Id, price);

            return order;
        }

        public async Task<Order?> PlaceTaxiOrderFromDiscord(ulong guildId, ulong discordId, long taxiId)
        {
            var taxi = await _taxiRepository.FindByTeleportIdAsync(taxiId);
            if (taxi is null) throw new DomainException("Taxi not found");

            var player = await _playerRepository.FindOneWithServerAsync(u => u.ScumServer.Guild != null && u.ScumServer.Guild.DiscordId == guildId && u.DiscordId == discordId);
            if (player is null) throw new DomainException("You are not registered, please register using the Welcome Pack.");

            var order = new Order
            {
                Taxi = taxi,
                TaxiTeleportId = taxiId.ToString(),
                Player = player,
                OrderType = EOrderType.Taxi,
                ScumServer = player.ScumServer
            };

            var processor = new OrderPurchaseProcessor(_orderRepository, _cacheService);
            await processor.ValidateAsync(order);
            var price = order.ResolvedPrice;

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();
            await new PlayerCoinManager(_unitOfWork).RemoveCoinsByPlayerId(player.Id, price);

            return order;
        }


        public async Task<List<GrapthDto>> GetBestSellingOrdersPacks()
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid token");

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Invalid server");

            var orders = await _unitOfWork
                .AppDbContext
                .Orders
                .Include(order => order.Pack)
                .Include(order => order.Warzone)
                .Include(order => order.ScumServer)
                .Where(order => order.ScumServer.Id == server.Id)
                .ToListAsync();

            var packs = orders
                .Where(order => order.OrderType == EOrderType.Pack && !order.Pack!.IsWelcomePack)
                .GroupBy(p => p.Pack!.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.Count(),
                    Color = ColorUtil.GetRandomColor()
                })
                .ToList();

            return packs.Select(a => new GrapthDto()
            {
                Color = a.Color,
                Amount = a.Count,
                Name = a.Name
            }).ToList();
        }

        public async Task ResetCommandOrders(long serverId)
        {
            var orders = await _orderRepository.FindManyCommandByServer(serverId);
            var timeToCancel = DateTime.UtcNow.AddMinutes(-30);
            var manager = new PlayerCoinManager(_unitOfWork);

            foreach (var order in orders)
            {
                if (timeToCancel > order.CreateDate)
                {
                    order.Status = EOrderStatus.Canceled;
                    await _orderRepository.CreateOrUpdateAsync(order);

                    if (order.Player != null)
                    {
                        var price = order.GetPrice();
                        await manager.AddCoinsByPlayerId(order.Player.Id, price);
                    }
                }
                else if (order.Status != EOrderStatus.Created)
                {
                    order.Status = EOrderStatus.Created;
                    await _orderRepository.CreateOrUpdateAsync(order);
                }
            }

            await _orderRepository.SaveAsync();
        }

        public async Task<Order> ExchangeDepositOrder(long serverId, ulong discordId, long amount)
        {
            var player = await _playerRepository.FindOneWithServerAsync(u =>
                u.ScumServer.Guild != null
                && u.ScumServer.Id == serverId
                && u.DiscordId == discordId);

            if (player is null) throw new DomainException("You are not registered, please register using the Welcome Pack.");

            if (player.ScumServer?.Exchange is null)
            {
                throw new DomainException("Invalid server");
            }

            if (player.Money < amount)
            {
                throw new DomainException("You don't have enough ingame money.");
            }

            var order = new Order
            {
                ExchangeAmount = amount,
                ExchangeType = EExchangeType.Deposit,
                Player = player,
                OrderType = EOrderType.Exchange,
                ScumServer = player.ScumServer
            };

            var processor = new OrderPurchaseProcessor(_orderRepository, _cacheService);
            await processor.ValidateAsync(order);

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();

            return order;
        }

        public async Task<Order> ExchangeWithdrawOrder(long serverId, ulong discordId, long amount)
        {
            var player = await _playerRepository.FindOneWithServerAsync(u =>
                u.ScumServer.Guild != null
                && u.ScumServer.Id == serverId
                && u.DiscordId == discordId);

            if (player is null) throw new DomainException("You are not registered, please register using the Welcome Pack.");

            if (player.ScumServer?.Exchange is null)
            {
                throw new DomainException("Invalid server");
            }

            if (player.Coin < amount)
            {
                throw new DomainException("You don't have enough ingame money.");
            }

            var order = new Order
            {
                ExchangeAmount = amount,
                ExchangeType = EExchangeType.Withdraw,
                Player = player,
                OrderType = EOrderType.Exchange,
                ScumServer = player.ScumServer
            };

            var processor = new OrderPurchaseProcessor(_orderRepository, _cacheService);
            await processor.ValidateAsync(order);

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();

            return order;
        }
    }
}
