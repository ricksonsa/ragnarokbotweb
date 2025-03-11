using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class OrderCommandJob : IJob
    {
        private readonly ICacheService _cacheService;
        private readonly IOrderRepository _orderRepository;
        private readonly IBotRepository _botRepository;

        public OrderCommandJob(
            ICacheService cacheService,
            IBotRepository botRepository,
            IOrderRepository orderRepository)
        {
            _cacheService = cacheService;
            _botRepository = botRepository;
            _orderRepository = orderRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var order = await _orderRepository.FindOneWithPackCreated();

            if (order is null) return;
            if (order.Pack is null) return;
            if ((await _botRepository.FindOneAsync(bot => bot.State == EBotState.Online)) is null) return;

            order.Status = EOrderStatus.Command;
            _orderRepository.Update(order);
            await _orderRepository.SaveAsync();

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
