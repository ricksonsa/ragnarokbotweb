using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Order?> PlaceOrder(string steamId64, long packId);
        Task<IEnumerable<Order>> GetCreatedOrders();
        Task<Page<OrderDto>> GetPacksPageByFilterAsync(Paginator paginator, string? filter);

    }
}
