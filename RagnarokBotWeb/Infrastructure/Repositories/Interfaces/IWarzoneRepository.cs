using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IWarzoneRepository : IRepository<Warzone>
    {
        Task<Warzone?> FindByIdAsNoTrackingAsync(long id);
        Task<List<Warzone>> FindActiveByServerId(long serverId);
        Task<Page<Warzone>> GetPageByServerAndFilter(Paginator paginator, long id, string? filter);
    }
}
