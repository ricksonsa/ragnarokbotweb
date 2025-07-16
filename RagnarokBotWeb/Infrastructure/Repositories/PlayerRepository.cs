using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class PlayerRepository : Repository<Player>, IPlayerRepository
{
    private readonly AppDbContext _appDbContext;

    public PlayerRepository(AppDbContext appDbContext) : base(appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public override Task<Player?> FindByIdAsync(long id)
    {
        return _appDbContext.Players
            .Include(player => player.Vips)
            .Include(player => player.Bans)
            .Include(player => player.Silences)
            .Include(player => player.ScumServer)
            .FirstOrDefaultAsync(player => player.Id == id);
    }

    public async Task<Player?> FindOneWithServerAsync(Expression<Func<Player, bool>> predicate)
    {
        return await _appDbContext.Players
          .Include(player => player.Vips)
          .Include(player => player.Bans)
          .Include(player => player.Silences)
          .Include(player => player.ScumServer)
          .Include(player => player.ScumServer.Guild)
          .FirstOrDefaultAsync(predicate);
    }

    public async Task<Player?> FindOneWithServerBySteamIdAsync(long serverId, string steamId64)
    {
        return await _appDbContext.Players
          .Include(player => player.Vips)
          .Include(player => player.Bans)
          .Include(player => player.Silences)
          .Include(player => player.ScumServer)
          .FirstOrDefaultAsync(player => player.ScumServerId == serverId && player.SteamId64 == steamId64);
    }

    public Task<List<Player>> GetAllByServerId(long serverId)
    {
        return _appDbContext.Players
           .Include(player => player.Vips)
           .Include(player => player.Bans)
           .Include(player => player.Silences)
           .Include(player => player.ScumServer)
           .Where(player => player.ScumServer.Id == serverId)
           .ToListAsync();
    }

    public Task<Page<Player>> GetPageByServerId(Paginator paginator, long serverId, string? filter)
    {
        var query = _appDbContext.Players
            .Include(player => player.Vips)
            .Include(player => player.Bans)
            .Include(player => player.Silences)
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

    public override Task CreateOrUpdateAsync(Player entity)
    {
        if (entity.ScumServer is not null) _appDbContext.ScumServers.Attach(entity.ScumServer);
        return base.CreateOrUpdateAsync(entity);
    }

}
