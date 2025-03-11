using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class PlayerRepository(AppDbContext appDbContext) : Repository<Player>(appDbContext), IPlayerRepository
{
    public Task<List<Player>> GetAllByServerId(long serverId)
    {
        return appDbContext.Players
           .Include(player => player.ScumServer)
           .Where(player => player.ScumServer.Id == serverId)
           .ToListAsync();
    }

    public Task<Page<Player>> GetPageByServerId(Paginator paginator, long serverId)
    {
        var query = appDbContext.Players
            .Include(player => player.ScumServer)
            .Where(player => player.ScumServer.Id == serverId);

        return base.GetPageAsync(paginator, query);
    }

}
