using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class ScumServerRepository : Repository<ScumServer>, IScumServerRepository
    {
        private readonly AppDbContext _appDbContext;
        public ScumServerRepository(AppDbContext context) : base(context) { _appDbContext = context; }

        public override Task<ScumServer?> FindByIdAsync(long id)
        {
            return _appDbContext.ScumServers
                .Include(server => server.Guild)
                .Include(server => server.Tenant)
                .Include(server => server.Ftp)
                .FirstOrDefaultAsync(server => server.Id == id);
        }

        public Task<List<ScumServer>> FindManyByTenantIdAsync(long id)
        {
            return _appDbContext.ScumServers
                .Include(server => server.Guild)
                .Include(server => server.Tenant)
                .Include(server => server.Ftp)
                .Where(server => server.Tenant.Id == id && server.Tenant.Enabled)
                .ToListAsync();
        }

        public Task<ScumServer?> FindOneByTenantIdAsync(long id)
        {
            return _appDbContext.ScumServers
               .Include(server => server.Guild)
               .Include(server => server.Tenant)
               .Include(server => server.Ftp)
               .FirstOrDefaultAsync(server => server.Tenant.Id == id && server.Tenant.Enabled);
        }

        public override Task CreateOrUpdateAsync(ScumServer entity)
        {
            _appDbContext.Tenants.Attach(entity.Tenant);
            return base.CreateOrUpdateAsync(entity);
        }

        public Task<List<ScumServer>> GetActiveServersWithFtp()
        {
            return _appDbContext.ScumServers
                .Include(server => server.Guild)
                .Include(server => server.Tenant)
                .Include(server => server.Ftp)
                .Where(server => server.Tenant.Enabled && server.Ftp != null)
                .ToListAsync();
        }

        public Task<ScumServer?> FindByIdAsNoTrackingAsync(long id)
        {
            return _appDbContext.ScumServers
                .AsNoTracking()
                .Include(server => server.Guild)
                .Include(server => server.Tenant)
                .Include(server => server.Ftp)
                .FirstOrDefaultAsync(server => server.Id == id);
        }

        public Task<ScumServer?> FindActiveById(long id)
        {
            return _appDbContext.ScumServers
              .Include(server => server.Guild)
              .Include(server => server.Tenant)
              .Include(server => server.Ftp)
              .FirstOrDefaultAsync(server => server.Id == id && server.Tenant.Enabled);
        }

        public Task<List<ScumServer>> FindActive()
        {
            return _appDbContext.ScumServers
               .Include(server => server.Tenant)
               .Where(server => server.Tenant.Enabled)
               .ToListAsync();
        }

        public Task<ScumServer?> FindByGuildId(ulong value)
        {
            return _appDbContext.ScumServers
            .Include(server => server.Guild)
            .Include(server => server.Tenant)
            .Include(server => server.Ftp)
            .FirstOrDefaultAsync(server => server.Guild != null && server.Guild.DiscordId == value && server.Tenant.Enabled);
        }
    }
}
