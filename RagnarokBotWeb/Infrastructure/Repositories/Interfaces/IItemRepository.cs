using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IItemRepository : IRepository<Item>
    {
        Task<Page<Item>> GetPageByFilter(Paginator paginator, string? filter);
    }
}
