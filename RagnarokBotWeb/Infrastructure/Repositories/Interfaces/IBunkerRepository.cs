using RagnarokBotWeb.Domain.Entities;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IBunkerRepository : IRepository<Bunker>
    {
        Task<Bunker?> FindOneWithServerAsync(Expression<Func<Bunker, bool>> predicate);
    }
}
