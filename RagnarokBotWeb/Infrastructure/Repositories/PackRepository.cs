using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class PackRepository : Repository<Pack>, IPackRepository
    {
        private readonly AppDbContext _appDbContext;
        public PackRepository(AppDbContext context) : base(context) { _appDbContext = context; }

        public override async Task<Pack?> FindByIdAsync(long id)
        {
            return await _appDbContext.Packs
                .Include(pack => pack.ScumServer)
                .Include(warzone => warzone.ScumServer.Tenant)
                .Include(warzone => warzone.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Include(pack => pack.ScumServer.Guild)
                .Include(pack => pack.PackItems)
                .ThenInclude(packItem => packItem.Item)
                .FirstOrDefaultAsync(pack => pack.Id == id);
        }

        public async Task<Pack?> FindWelcomePackByServerIdAsync(long id)
        {
            return await _appDbContext.Packs
                .Include(pack => pack.ScumServer)
                .Include(warzone => warzone.ScumServer.Tenant)
                .Include(warzone => warzone.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Include(pack => pack.PackItems)
                .ThenInclude(packItem => packItem.Item)
                .FirstOrDefaultAsync(pack => pack.ScumServer.Id == id && pack.IsWelcomePack && pack.Enabled);
        }

        public async Task<Pack?> FindByIdAsNoTrackingAsync(long id)
        {
            return await _appDbContext.Packs
                .Include(pack => pack.ScumServer)
                .Include(warzone => warzone.ScumServer.Tenant)
                .Include(warzone => warzone.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
                .Include(pack => pack.PackItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(pack => pack.Id == id);
        }

        public override async Task<IEnumerable<Pack>> GetAllAsync()
        {
            return await _appDbContext.Packs
               .Include(pack => pack.ScumServer)
               .Include(warzone => warzone.ScumServer.Tenant)
                .Include(warzone => warzone.ScumServer.Tenant.Payments)
                    .ThenInclude(payment => payment.Subscription)
               .Include(pack => pack.PackItems)
               .ThenInclude(packItem => packItem.Item)
               .ToListAsync();
        }

        public Task<Page<Pack>> GetPageByServerAndFilter(Paginator paginator, long id, string? filter)
        {
            var query = _appDbContext.Packs
                .Include(pack => pack.ScumServer)
                .Include(pack => pack.PackItems)
                .ThenInclude(packItem => packItem.Item)
                .OrderByDescending(pack => pack.Id)
                .Where(pack => pack.Deleted == null && pack.ScumServer.Id == id && !pack.IsWelcomePack);

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                return base.GetPageAsync(paginator, query.Where(pack => pack.Name.ToLower().Contains(filter)
                || pack.Description != null && pack.Description.ToLower().Contains(filter)));
            }

            return base.GetPageAsync(paginator, query);
        }

        public override Task CreateOrUpdateAsync(Pack entity)
        {
            foreach (var packItem in entity.PackItems)
            {
                _appDbContext.Items.Attach(packItem.Item);
            }
            return base.CreateOrUpdateAsync(entity);
        }
    }
}
