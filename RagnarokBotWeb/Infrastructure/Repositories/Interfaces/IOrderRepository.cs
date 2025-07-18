using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> FindOneByServer(long serverId);
        Task<List<Order>> FindWithPack(long packId);
        Task<List<Order>> FindWithWarzone(long warzoneId);
        Task<Page<Order>> GetPageByFilter(Paginator paginator, string? filter);
    }
}
