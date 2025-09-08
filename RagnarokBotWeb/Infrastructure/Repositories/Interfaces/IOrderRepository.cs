using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> FindOneByServer(long serverId);
        Task<List<Order>> FindWithPack(long packId);
        Task<List<Order>> FindWithWarzone(long warzoneId);
        Task<List<Order>> FindWithTaxi(long taxiId);
        Task<Page<Order>> GetPageByFilter(long serverId, Paginator paginator, string? filter);
        Task<List<Order>> FindManyByServerForCommand(long serverId);
        Task<List<Order>> FindManyCommandByServer(long serverId);
        Task<List<Order>> FindManyForProcessor(Expression<Func<Order, bool>> predicate);
    }
}
