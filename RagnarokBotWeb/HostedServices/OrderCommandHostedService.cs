using RagnarokBotWeb.Application;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.HostedServices
{
    public class OrderCommandHostedService : TimedHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheService _cacheService;
        public OrderCommandHostedService(IServiceProvider serviceProvider, ICacheService cacheService) : base(TimeSpan.FromSeconds(15))
        {
            _serviceProvider = serviceProvider;
            _cacheService = cacheService;
        }

        public override async Task Process()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var botRepository = scope.ServiceProvider.GetRequiredService<IBotRepository>();
                var order = await orderRepository.FindOneWithPackCreated();

                if (order is null) return;
                if (order.Pack is null) return;
                if ((await botRepository.FindOneAsync(bot => bot.State == EBotState.Online)) is null) return;

                order.Status = EOrderStatus.Command;
                orderRepository.Update(order);
                await orderRepository.SaveAsync();

                var commands = new List<BotCommand>();
                foreach (var packItem in order.Pack.PackItems)
                {
                    if (order.Player?.SteamId64 is null) continue;
                    commands.Add(BotCommand.Delivery(order.Player.SteamId64, packItem.Item.Code, packItem.Amount));
                }

                commands.ForEach(_cacheService.GetCommandQueue(order.ScumServer.Id).Enqueue);
            }
        }
    }
}
