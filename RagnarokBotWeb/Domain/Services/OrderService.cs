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
            ITaxiRepository taxiRepository) : base(contextAccessor)
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

        public async Task<Order?> PlaceUavOrderFromDiscord(ScumServer server, ulong userDiscordId, string sector)
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
            foreach (var order in orders)
            {
                order.Status = EOrderStatus.Created;
                await _orderRepository.CreateOrUpdateAsync(order);
                await _orderRepository.SaveAsync();
            }
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

            if (player.Coin < amount)
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
