using Quartz;
using RagnarokBotWeb.Application.Handlers;
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
        private readonly IDiscordService _discordService;
        private readonly IFileService _fileService;
        private readonly IBotService _botService;

        public OrderCommandJob(
            ICacheService cacheService,
            IScumServerRepository scumServerRepository,
            IBotService botService,
            IOrderRepository orderRepository,
            ILogger<OrderCommandJob> logger,
            IDiscordService discordService,
            IFileService fileService) : base(scumServerRepository)
        {
            _cacheService = cacheService;
            _botService = botService;
            _orderRepository = orderRepository;
            _logger = logger;
            _discordService = discordService;
            _fileService = fileService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            var server = await GetServerAsync(context, ftpRequired: false, validateSubscription: true);
            var orders = await _orderRepository.FindManyByServer(server.Id);

            if (orders.Count == 0) return;
            if (!_botService.IsBotOnline(server.Id)) return;

            var players = _cacheService.GetConnectedPlayers(server.Id);

            foreach (var order in orders)
            {
                if (order.Player?.SteamId64 is null) continue;
                if (!players.Any(player => player.SteamID == order.Player.SteamId64)) continue;

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

                    var deliveryText = order.ResolvedDeliveryText();
                    if (deliveryText is not null) command.Say(deliveryText);

                    command.Data = "order_" + order.Id.ToString();
                }
                else if (order.OrderType == EOrderType.Warzone)
                {
                    if (order.Warzone is null) return;
                    var teleport = WarzoneRandomSelector.SelectTeleportPoint(order.Warzone!);
                    command.Teleport(order.Player.SteamId64, teleport.Teleport.Coordinates);
                }
                else if (order.OrderType == EOrderType.UAV)
                {
                    if (order.ScumServer?.Uav is null) return;

                    await new UavHandler(
                        _discordService,
                        _fileService,
                        _cacheService,
                        server).Handle(order.Player, order.Uav!);

                    var deliveryText = order.ResolvedDeliveryText();
                    if (deliveryText is not null) command.Say(deliveryText);

                    order.Status = EOrderStatus.Done;
                    _orderRepository.Update(order);
                    await _orderRepository.SaveAsync();
                }

                if (command != null) _cacheService.GetCommandQueue(order.ScumServer.Id).Enqueue(command);

            }


        }
    }
}
