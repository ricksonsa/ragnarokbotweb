using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Order?> PlaceOrder(string steamId64, long packId);
        Task<IEnumerable<Order>> GetCreatedOrders();
        Task<List<Command>> GetCommand(long botId);
    }
}
