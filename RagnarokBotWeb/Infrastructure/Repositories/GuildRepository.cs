using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class GuildRepository(AppDbContext appDbContext) : Repository<Guild>(appDbContext), IGuildRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public Task<Guild?> FindOneWithScumServerAsync(Expression<Func<Guild, bool>> predicate)
    {
        return _appDbContext.Guilds
            .Include(guild => guild.ScumServer)
            .Where(predicate)
            .FirstOrDefaultAsync();
    }
}