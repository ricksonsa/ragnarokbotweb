using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IPackRepository : IRepository<Pack>
    {
        Task<Page<Pack>> GetPageByFilter(Paginator paginator, string? filter);
        Task<Pack?> FindByIdAsNoTrackingAsync(long id);
    }
}
