using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
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
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IPlayerRepository _playerRepository;

        public OrderService(
            IHttpContextAccessor contextAccessor,
            ILogger<OrderService> logger,
            IOrderRepository orderRepository,
            IPackRepository packRepository,
            IPlayerRepository userRepository,
            IScumServerRepository scumServerRepository) : base(contextAccessor)
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _packRepository = packRepository;
            _playerRepository = userRepository;
            _scumServerRepository = scumServerRepository;
        }

        public Task<IEnumerable<Order>> GetCreatedOrders()
        {
            return _orderRepository.FindAsync(order => order.Status == EOrderStatus.Created);
        }

        public async Task<Order?> PlaceOrder(string steamId64, long packId)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid scum server");

            var server = await _scumServerRepository.FindByIdAsync(serverId.Value);
            if (server is null) throw new DomainException("Scum server not found");

            var pack = await _packRepository.FindByIdAsync(packId);
            if (pack is null) throw new DomainException("Pack not found");

            var player = await _playerRepository.FindOneAsync(u => u.SteamId64 == steamId64);
            if (player is null) throw new DomainException("Player not found");

            var order = new Order
            {
                Pack = pack,
                Player = player,
                ScumServer = server
            };

            await _orderRepository.CreateOrUpdateAsync(order);
            await _orderRepository.SaveAsync();

            return order;
        }
    }
}
