using AutoMapper;
using RagnarokBotWeb.Application.Pagination;
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
        private readonly IMapper _mapper;

        public OrderService(
            IHttpContextAccessor contextAccessor,
            ILogger<OrderService> logger,
            IOrderRepository orderRepository,
            IPackRepository packRepository,
            IPlayerRepository userRepository,
            IMapper mapper,
            IWarzoneRepository warzoneRepository) : base(contextAccessor)
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _packRepository = packRepository;
            _playerRepository = userRepository;
            _mapper = mapper;
            _warzoneRepository = warzoneRepository;
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
            Page<Order> page = await _orderRepository.GetPageByFilter(paginator, filter);
            return new Page<OrderDto>(page.Content.Select(_mapper.Map<OrderDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
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
            if (pack is null) throw new DomainException("Pack not found");

            var player = await _playerRepository.FindOneWithServerAsync(u => u.SteamId64 == identifier || u.DiscordId.ToString() == identifier);
            if (player is null) throw new NotFoundException("Player not found");

            var order = new Order
            {
                Pack = pack,
                Player = player,
                OrderType = EOrderType.Pack,
                ScumServer = player.ScumServer
            };

            var processor = new OrderPurchaseProcessor(_orderRepository);
            await processor.ValidateAsync(order);
            processor.Charge(order);

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();

            return order;
        }

        public async Task<Order?> PlaceDeliveryOrderFromDiscord(ulong guildId, ulong discordId, long packId)
        {
            var pack = await _packRepository.FindByIdAsync(packId);
            if (pack is null) throw new DomainException("Pack not found");

            var player = await _playerRepository.FindOneWithServerAsync(u => u.ScumServer.Guild != null && u.ScumServer.Guild.DiscordId == guildId && u.DiscordId == discordId);
            if (player is null) throw new NotFoundException("Player not found");

            var order = new Order
            {
                Pack = pack,
                Player = player,
                OrderType = EOrderType.Pack,
                ScumServer = player.ScumServer
            };

            await new OrderPurchaseProcessor(_orderRepository).ValidateAsync(order);
            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();

            return order;
        }

        public async Task<Order?> PlaceWarzoneOrderFromDiscord(ulong guildId, ulong discordId, long warzoneId)
        {
            var warzone = await _warzoneRepository.FindByIdAsync(warzoneId);
            if (warzone is null) throw new DomainException("Warzone not found");

            var player = await _playerRepository.FindOneWithServerAsync(u => u.ScumServer.Guild != null && u.ScumServer.Guild.DiscordId == guildId && u.DiscordId == discordId);
            if (player is null) throw new NotFoundException("Player not found");

            var order = new Order
            {
                Warzone = warzone,
                Player = player,
                OrderType = EOrderType.Warzone,
                ScumServer = player.ScumServer
            };

            await new OrderPurchaseProcessor(_orderRepository).ValidateAsync(order);
            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();

            return order;
        }
    }
}
