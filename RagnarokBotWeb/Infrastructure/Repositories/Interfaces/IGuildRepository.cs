using System.Linq.Expressions;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

public interface IGuildRepository : IRepository<Guild>
{
    Task<Guild?> FindOneWithScumServerAsync(Expression<Func<Guild, bool>> predicate);
}