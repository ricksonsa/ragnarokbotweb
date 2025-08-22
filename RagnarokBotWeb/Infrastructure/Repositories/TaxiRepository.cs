using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class TaxiRepository : Repository<Taxi>, ITaxiRepository
    {
        private readonly AppDbContext _appDbContext;
        public TaxiRepository(AppDbContext context) : base(context)
        {
            _appDbContext = context;
        }

        public override Task CreateOrUpdateAsync(Taxi entity)
        {
            return base.CreateOrUpdateAsync(entity);
        }

        public Task<List<Taxi>> FindActiveByServerId(long serverId)
        {
            return DbSet()
                .Include(taxi => taxi.ScumServer)
                .Include(taxi => taxi.ScumServer.Guild)
                .Include(taxi => taxi.ScumServer.Tenant)
                .Include(taxi => taxi.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Where(taxi => taxi.Enabled && taxi.ScumServer.Id == serverId && taxi.Deleted == null)
                .ToListAsync();
        }

        public Task<Taxi?> FindByIdAsNoTrackingAsync(long id)
        {
            return DbSet()
                .Include(taxi => taxi.ScumServer)
                .Include(taxi => taxi.ScumServer.Guild)
                .Include(taxi => taxi.ScumServer.Tenant)
                .Include(taxi => taxi.ScumServer.Tenant.Payments)
                    .ThenInclude(taxi => taxi.Subscription)
                .Include(taxi => taxi.TaxiTeleports)
                    .ThenInclude(taxi => taxi.Teleport)
                .AsNoTracking()
                .FirstOrDefaultAsync(taxi => taxi.Id == id);
        }

        public override Task<Taxi?> FindByIdAsync(long id)
        {
            return DbSet()
                .Include(taxi => taxi.ScumServer)
                .Include(taxi => taxi.ScumServer.Guild)
                .Include(taxi => taxi.ScumServer.Tenant)
                .Include(taxi => taxi.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Include(taxi => taxi.TaxiTeleports)
                    .ThenInclude(taxi => taxi.Teleport)
                .FirstOrDefaultAsync(taxi => taxi.Id == id);
        }

        public Task<Taxi?> FindByTeleportIdAsync(long id)
        {
            return DbSet()
                .Include(taxi => taxi.ScumServer)
                .Include(taxi => taxi.ScumServer.Guild)
                .Include(taxi => taxi.ScumServer.Tenant)
                .Include(taxi => taxi.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Include(taxi => taxi.TaxiTeleports)
                    .ThenInclude(taxi => taxi.Teleport)
                .FirstOrDefaultAsync(taxi => taxi.TaxiTeleports.Any(tp => tp.Id == id));
        }

        public Task<Page<Taxi>> GetPageByServerAndFilter(Paginator paginator, long serverId, string? filter)
        {
            var query = DbSet()
                .Include(taxi => taxi.ScumServer)
                .Include(taxi => taxi.TaxiTeleports)
                    .ThenInclude(taxiTeleport => taxiTeleport.Teleport)
                .Where(taxi => taxi.Deleted == null && taxi.ScumServer.Id == serverId);

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                return base.GetPageAsync(paginator, query.Where(warzone => warzone.Name.ToLower().Contains(filter) ||
                (warzone.Description != null && warzone.Description.ToLower().Contains(filter))));
            }

            return base.GetPageAsync(paginator, query);
        }
    }
}
