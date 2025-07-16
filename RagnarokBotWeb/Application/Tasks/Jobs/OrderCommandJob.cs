using Quartz;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class OrderCommandJob : AbstractJob, IJob
    {
        private readonly ICacheService _cacheService;
        private readonly IOrderRepository _orderRepository;
        private readonly IBotRepository _botRepository;

        public OrderCommandJob(
            ICacheService cacheService,
            IScumServerRepository scumServerRepository,
            IBotRepository botRepository,
            IOrderRepository orderRepository) : base(scumServerRepository)
        {
            _cacheService = cacheService;
            _botRepository = botRepository;
            _orderRepository = orderRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var server = await GetServerAsync(context);
            var order = await _orderRepository.FindOneWithPackCreatedByServer(server.Id);

            if (order is null) return;
            if (order.Pack is null) return;
            if ((await _botRepository.FindByOnlineScumServerId(server.Id)) is null) return;
            if (order.Player?.SteamId64 is null) return;

            order.Status = EOrderStatus.Command;
            _orderRepository.Update(order);
            await _orderRepository.SaveAsync();

            var command = new BotCommand();
            foreach (var packItem in order.Pack.PackItems)
            {
                command.Delivery(order.Player.SteamId64, packItem.Item.Code, packItem.Amount);
            }

            command.Say(order.ResolvedDeliveryText());
            command.Data = "order_" + order.Id.ToString();

            _cacheService.GetCommandQueue(order.ScumServer.Id).Enqueue(command);
        }
    }
}
