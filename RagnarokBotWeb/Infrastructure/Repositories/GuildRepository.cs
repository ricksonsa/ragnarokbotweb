using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

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

    public override Task CreateOrUpdateAsync(Guild entity)
    {
        if (entity.ScumServer is not null) _appDbContext.ScumServers.Attach(entity.ScumServer);
        return base.CreateOrUpdateAsync(entity);
    }

    public Task<Guild?> FindByServerIdAsync(long id)
    {
        return _appDbContext.Guilds
         .Include(guild => guild.ScumServer)
         .Include(guild => guild.Channels)
            .ThenInclude(guildChannels => guildChannels.Buttons)
         .Where(guild => guild.ScumServer.Id == id)
         .FirstOrDefaultAsync();
    }
}