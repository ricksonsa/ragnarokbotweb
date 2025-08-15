using Quartz;
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

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            try
            {
                JobDataMap dataMap = context.JobDetail.JobDataMap;
                long? serverId = dataMap.GetLong("server_id");

                if (!serverId.HasValue)
                {
                    _logger.LogError("No value for variable serverId");
                    return;
                }

                await _orderService.ResetCommandOrders(serverId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
