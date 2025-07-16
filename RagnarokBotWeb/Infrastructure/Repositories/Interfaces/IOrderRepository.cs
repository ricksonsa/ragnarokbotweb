using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> FindOneWithPackCreatedByServer(long serverId);
        Task<List<Order>> FindWithPack(long packId);
        Task<Page<Order>> GetPageByFilter(Paginator paginator, string? filter);
    }
}
