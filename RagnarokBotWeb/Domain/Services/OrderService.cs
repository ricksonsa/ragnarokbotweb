using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class OrderService : IOrderService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly IOrderRepository _orderRepository;
        private readonly IPackRepository _packRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBotRepository _botRepository;

        public OrderService(ILogger<OrderService> logger,
            IOrderRepository orderRepository,
            IPackRepository packRepository,
            IUserRepository userRepository,
            IBotRepository botRepository)
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _packRepository = packRepository;
            _userRepository = userRepository;
            _botRepository = botRepository;
        }

        public Task<IEnumerable<Order>> GetCreatedOrders()
        {
            return _orderRepository.FindAsync(order => order.Status == Enums.EOrderStatus.Created);
        }

        public async Task<List<Command>> GetCommand(long botId)
        {
            var order = await _orderRepository.FindOneWithPackCreated();
            if (order is null) return null;
            var bot = await _botRepository.GetByIdAsync(botId);
            if (bot is null || !bot.Active) return null;
            order.Status = Enums.EOrderStatus.Command;
            _orderRepository.Update(order);
            await _orderRepository.SaveAsync();

            var commands = new List<Command>();
            order.Pack!.PackItems.ForEach(packItem =>
            {
                commands.Add(new Command
                {
                    Bot = bot,
                    Target = order.User!.SteamId64,
                    Type = Enums.ECommandType.Delivery,
                    Value = packItem.Item.Code,
                    Amount = packItem.Amount
                });
            });

            return commands;
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
