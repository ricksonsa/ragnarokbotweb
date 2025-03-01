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

        public Task<List<ScumServer>> FindByTenantIdAsync(long id)
        {
            return _appDbContext.ScumServers
                .Include(server => server.Guild)
                .Include(server => server.Tenant)
                .Include(server => server.Ftp)
                .Where(server => server.Tenant.Id == id)
                .ToListAsync();
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
    }
}
