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
        return DbSet()
            .Include(player => player.Vips)
            .Include(player => player.Bans)
            .Include(player => player.Silences)
            .Include(player => player.ScumServer)
            .Include(player => player.ScumServer.Exchange)
            .Include(player => player.ScumServer.Tenant)
            .Include(player => player.ScumServer.Tenant.Payments)
                .ThenInclude(payment => payment.Subscription)
            .Include(player => player.ScumServer.Guild)
            .FirstOrDefaultAsync(player => player.Id == id);
    }

    public async Task<Player?> FindOneWithServerAsync(Expression<Func<Player, bool>> predicate)
    {
        return await DbSet()
            .Include(player => player.Vips)
            .Include(player => player.Bans)
            .Include(player => player.Silences)
            .Include(player => player.ScumServer)
            .Include(player => player.ScumServer.Exchange)
            .Include(warzone => warzone.ScumServer.Tenant)
            .Include(warzone => warzone.ScumServer.Tenant.Payments)
                .ThenInclude(payment => payment.Subscription)
            .Include(player => player.ScumServer.Guild)
            .FirstOrDefaultAsync(predicate);
    }

    public async Task<Player?> FindOneWithServerBySteamIdAsync(long serverId, string steamId64)
    {
        return await DbSet()
            .Include(player => player.Vips)
            .Include(player => player.Bans)
            .Include(player => player.Silences)
            .Include(player => player.ScumServer)
            .Include(player => player.ScumServer.Exchange)
            .Include(player => player.ScumServer.Guild)
            .Include(warzone => warzone.ScumServer.Tenant)
            .Include(warzone => warzone.ScumServer.Tenant.Payments)
                .ThenInclude(payment => payment.Subscription)
            .FirstOrDefaultAsync(player => player.ScumServerId == serverId && player.SteamId64 == steamId64);
    }

    public Task<List<Player>> GetAllByServerId(long serverId)
    {
        return DbSet()
            .Include(player => player.Vips)
            .Include(player => player.Bans)
            .Include(player => player.Silences)
            .Include(player => player.ScumServer)
            .Include(player => player.ScumServer.Exchange)
            .Where(player => player.ScumServer.Id == serverId)
            .ToListAsync();
    }

    public Task<Page<Player>> GetPageByServerId(Paginator paginator, long serverId, string? filter)
    {
        var query = DbSet()
            .Include(player => player.Vips)
            .Include(player => player.Bans)
            .Include(player => player.Silences)
            .Include(player => player.ScumServer)
            .Include(player => player.ScumServer.Exchange)
            .OrderByDescending(player => player.Id)
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

    public Task<Page<Player>> GetVipPageByServerId(Paginator paginator, long serverId, string? filter)
    {
        var query = DbSet()
            .Include(player => player.Vips)
            .Include(player => player.Bans)
            .Include(player => player.Silences)
            .Include(player => player.ScumServer)
            .Include(player => player.ScumServer.Exchange)
            .OrderByDescending(player => player.Id)
            .Where(player => player.ScumServer.Id == serverId
                && player.Vips.Any(vip => vip.Indefinitely || vip.ExpirationDate.HasValue && vip.ExpirationDate.Value.Date > DateTime.UtcNow.Date && !vip.Processed));

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

    public Task<int> GetCount(long serverId)
    {
        var query = DbSet()
            .Include(player => player.Vips)
            .Include(player => player.ScumServer)
            .Where(player => player.ScumServer.Id == serverId);

        return query.CountAsync();
    }

    public Task<int> GetVipCount(long serverId)
    {
        var query = DbSet()
            .Include(player => player.Vips)
            .Include(player => player.ScumServer)
            .Where(player => player.ScumServer.Id == serverId
                && player.Vips.Any(vip => vip.Indefinitely || vip.ExpirationDate.HasValue && vip.ExpirationDate.Value.Date > DateTime.UtcNow.Date && !vip.Processed));

        return query.CountAsync();
    }

    public override Task CreateOrUpdateAsync(Player entity)
    {
        if (entity.ScumServer is not null) _appDbContext.ScumServers.Attach(entity.ScumServer);
        return base.CreateOrUpdateAsync(entity);
    }

}
