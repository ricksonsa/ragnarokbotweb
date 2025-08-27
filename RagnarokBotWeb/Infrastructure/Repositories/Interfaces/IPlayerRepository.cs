using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IPlayerRepository : IRepository<Player>
    {
        Task<Player?> FindOneWithServerAsync(Expression<Func<Player, bool>> predicate);
        Task<Player?> FindOneWithServerBySteamIdAsync(long serverId, string steamId64);
        Task<Page<Player>> GetPageByServerId(Paginator paginator, long serverId, string? filter);
        Task<Page<Player>> GetVipPageByServerId(Paginator paginator, long serverId, string? filter);
        Task<List<Player>> GetAllByServerId(long serverId);
        Task<int> GetCount(long serverId);
        Task<int> GetVipCount(long serverId);
    }
}
