using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Handlers
{
    public class ConfirmOrderCommand : IExclamationCommandHandler
    {
        private readonly IOrderService _orderService;

        public ConfirmOrderCommand(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task ExecuteAsync(ChatTextParseResult value)
        {
            var orderId = long.Parse(value.Text.Split("_")[1]);
            await _orderService.ConfirmOrderDelivered(orderId);
        }
    }
}
