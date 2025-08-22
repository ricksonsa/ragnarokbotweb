using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface ITaxiRepository : IRepository<Taxi>
    {
        Task<Taxi?> FindByIdAsNoTrackingAsync(long id);
        Task<List<Taxi>> FindActiveByServerId(long serverId);
        Task<Page<Taxi>> GetPageByServerAndFilter(Paginator paginator, long id, string? filter);
        Task<Taxi?> FindByTeleportIdAsync(long id);
    }
}
