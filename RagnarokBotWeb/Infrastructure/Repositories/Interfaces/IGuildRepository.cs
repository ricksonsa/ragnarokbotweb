using RagnarokBotWeb.Domain.Entities;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

public interface IGuildRepository : IRepository<Guild>
{
    Task<Guild?> FindOneWithScumServerAsync(Expression<Func<Guild, bool>> predicate);
    Task<Guild?> FindByServerIdAsync(long id);
}