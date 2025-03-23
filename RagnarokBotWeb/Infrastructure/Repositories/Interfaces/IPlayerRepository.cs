using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IPlayerRepository : IRepository<Player>
    {
        Task<Player?> FindOneWithServerAsync(Expression<Func<Player, bool>> predicate);
        Task<Page<Player>> GetPageByServerId(Paginator paginator, long serverId, string? filter);
        Task<List<Player>> GetAllByServerId(long serverId);
    }
}
