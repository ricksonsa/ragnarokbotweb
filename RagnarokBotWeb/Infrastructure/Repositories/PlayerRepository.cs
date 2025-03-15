using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class PlayerRepository(AppDbContext appDbContext) : Repository<Player>(appDbContext), IPlayerRepository
{
    public override Task<Player?> FindByIdAsync(long id)
    {
        return appDbContext.Players
            .Include(player => player.ScumServer)
            .FirstOrDefaultAsync(player => player.Id == id);
    }

    public Task<List<Player>> GetAllByServerId(long serverId)
    {
        return appDbContext.Players
           .Include(player => player.ScumServer)
           .Where(player => player.ScumServer.Id == serverId)
           .ToListAsync();
    }

    public Task<Page<Player>> GetPageByServerId(Paginator paginator, long serverId, string? filter)
    {
        var query = appDbContext.Players
            .Include(player => player.ScumServer)
            .Where(player => player.ScumServer.Id == serverId);

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(player =>
               player.Name!.ToLower().Contains(filter.ToLower())
               || (player.DiscordName != null && player.DiscordName.ToLower().Contains(filter.ToLower()))
               || (player.SteamId64 != null && player.SteamId64.ToLower().Contains(filter.ToLower()))
               || (player.SteamName != null && player.SteamName.ToLower().Contains(filter.ToLower())));
        }

        return base.GetPageAsync(paginator, query);
    }

}
