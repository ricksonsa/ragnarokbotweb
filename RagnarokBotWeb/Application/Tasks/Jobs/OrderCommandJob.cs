using Quartz;
using RagnarokBotWeb.Domain.Business;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Enums;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class OrderCommandJob : AbstractJob, IJob
    {
        private readonly ILogger<OrderCommandJob> _logger;
        private readonly ICacheService _cacheService;
        private readonly IOrderRepository _orderRepository;
        private readonly IBotRepository _botRepository;

        public OrderCommandJob(
            ICacheService cacheService,
            IScumServerRepository scumServerRepository,
            IBotRepository botRepository,
            IOrderRepository orderRepository,
            ILogger<OrderCommandJob> logger) : base(scumServerRepository)
        {
            _cacheService = cacheService;
            _botRepository = botRepository;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false);
            var order = await _orderRepository.FindOneByServer(server.Id);

            if (order is null) return;
            if ((await _botRepository.FindByOnlineScumServerId(server.Id)) is null) return;
            if (order.Player?.SteamId64 is null) return;

            order.Status = EOrderStatus.Command;
            _orderRepository.Update(order);
            await _orderRepository.SaveAsync();

            var command = new BotCommand();

            if (order.OrderType == EOrderType.Pack)
            {
                if (order.Pack is null) return;
                foreach (var packItem in order.Pack.PackItems)
                {
                    if (packItem.AmmoCount > 0)
                        command.MagazineDelivery(order.Player.SteamId64, packItem.Item.Code, packItem.Amount, packItem.AmmoCount);
                    else
                        command.Delivery(order.Player.SteamId64, packItem.Item.Code, packItem.Amount);
                }

                command.Say(order.ResolvedDeliveryText());
                command.Data = "order_" + order.Id.ToString();
            }
            else if (order.OrderType == EOrderType.Warzone)
            {
                if (order.Warzone is null) return;
                var teleport = WarzoneRandomSelector.SelectTeleportPoint(order.Warzone!);
                command.Teleport(order.Player.SteamId64, teleport.Teleport.Coordinates);
            }

            if (command != null) _cacheService.GetCommandQueue(order.ScumServer.Id).Enqueue(command);
        }
    }
}
