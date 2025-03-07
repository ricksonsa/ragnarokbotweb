using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IPlayerRepository : IRepository<Player>
    {
        Task<Page<Player>> GetPageByServerId(Paginator paginator, long serverId);
    }
}
