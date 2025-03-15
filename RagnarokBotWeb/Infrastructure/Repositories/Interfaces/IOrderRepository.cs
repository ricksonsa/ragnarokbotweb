using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> FindOneWithPackCreated();
        Task<Page<Order>> GetPageByFilter(Paginator paginator, string? filter);
    }
}
