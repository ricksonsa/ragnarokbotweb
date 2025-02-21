using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.Domain.Services
{
    public class OrderService : IOrderService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly IOrderRepository _orderRepository;
        private readonly IPackRepository _packRepository;
        private readonly IUserRepository _userRepository;

        public OrderService(ILogger<OrderService> logger,
            IOrderRepository orderRepository,
            IPackRepository packRepository,
            IUserRepository userRepository)
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _packRepository = packRepository;
            _userRepository = userRepository;
        }

        public Task<IEnumerable<Order>> GetCreatedOrders()
        {
            return _orderRepository.FindAsync(order => order.Status == EOrderStatus.Created);
        }

        public async Task<Order?> PlaceOrder(string steamId64, long packId)
        {
            var pack = await _packRepository.GetByIdAsync(packId);
            if (pack is null) return null;

            var user = await _userRepository.FindOneAsync(u => u.SteamId64 == steamId64);
            if (user is null) return null;

            var order = new Order
            {
                Pack = pack,
                User = user
            };

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveAsync();

            return order;
        }
    }
}
