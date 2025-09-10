using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class OrderResetJob : IJob
    {
        private readonly ILogger<OrderResetJob> _logger;
        private readonly IOrderService _orderService;

        public OrderResetJob(ILogger<OrderResetJob> logger, IOrderService orderService)
        {
            _logger = logger;
            _orderService = orderService;
        }

        public async Task Execute(long serverId)
        {
            _logger.LogInformation("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

            try
            {
                await _orderService.ResetCommandOrders(serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
