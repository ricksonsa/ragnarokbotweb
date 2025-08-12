using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class WarzoneRepository : Repository<Warzone>, IWarzoneRepository
    {
        private readonly AppDbContext _appDbContext;
        public WarzoneRepository(AppDbContext context) : base(context)
        {
            _appDbContext = context;
        }

        public override Task CreateOrUpdateAsync(Warzone entity)
        {
            entity.WarzoneItems.ForEach(wi => _appDbContext.Items.Attach(wi.Item));
            return base.CreateOrUpdateAsync(entity);
        }

        public Task<List<Warzone>> FindActiveByServerId(long serverId)
        {
            return DbSet()
                .Include(warzone => warzone.ScumServer)
                .Include(warzone => warzone.ScumServer.Tenant)
                .Include(warzone => warzone.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Where(warzone => warzone.Enabled && warzone.ScumServer.Id == serverId && warzone.Deleted == null)
                .ToListAsync();
        }

        public Task<Warzone?> FindByIdAsNoTrackingAsync(long id)
        {
            return DbSet()
                .Include(warzone => warzone.ScumServer)
                .Include(warzone => warzone.ScumServer.Tenant)
                .Include(warzone => warzone.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Include(warzone => warzone.WarzoneItems)
                    .ThenInclude(warzone => warzone.Item)
                .Include(warzone => warzone.SpawnPoints)
                    .ThenInclude(warzone => warzone.Teleport)
                .Include(warzone => warzone.Teleports)
                    .ThenInclude(warzone => warzone.Teleport)
                .AsNoTracking()
                .FirstOrDefaultAsync(warzone => warzone.Id == id);
        }

        public override Task<Warzone?> FindByIdAsync(long id)
        {
            return DbSet()
                .Include(warzone => warzone.ScumServer)
                .Include(warzone => warzone.ScumServer.Tenant)
                .Include(warzone => warzone.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Include(warzone => warzone.WarzoneItems)
                    .ThenInclude(warzone => warzone.Item)
                .Include(warzone => warzone.SpawnPoints)
                    .ThenInclude(warzone => warzone.Teleport)
                .Include(warzone => warzone.Teleports)
                    .ThenInclude(warzone => warzone.Teleport)
                .FirstOrDefaultAsync(warzone => warzone.Id == id);
        }

        public Task<Page<Warzone>> GetPageByServerAndFilter(Paginator paginator, long serverId, string? filter)
        {
            var query = DbSet()
                .Include(warzone => warzone.ScumServer)
                .Include(warzone => warzone.WarzoneItems)
                    .ThenInclude(warzone => warzone.Item)
                .Include(warzone => warzone.SpawnPoints)
                    .ThenInclude(warzone => warzone.Teleport)
                .Include(warzone => warzone.Teleports)
                    .ThenInclude(warzone => warzone.Teleport)
                .Where(warzone => warzone.Deleted == null && warzone.ScumServer.Id == serverId);

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                return base.GetPageAsync(paginator, query.Where(warzone => warzone.Name.ToLower().Contains(filter) || warzone.Description.ToLower().Contains(filter)));
            }

            return base.GetPageAsync(paginator, query);
        }
    }
}
