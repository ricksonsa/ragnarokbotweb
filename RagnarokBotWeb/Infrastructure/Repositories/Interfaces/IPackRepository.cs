using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IPackRepository : IRepository<Pack>
    {
        Task<Page<Pack>> GetPageByServerAndFilter(Paginator paginator, long id, string? filter);
        Task<Pack?> FindByIdAsNoTrackingAsync(long id);
        Task<Pack?> FindWelcomePackByServerIdAsync(long id);
    }
}
