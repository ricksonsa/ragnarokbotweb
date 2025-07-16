using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class BunkerRepository : Repository<Bunker>, IBunkerRepository
    {
        private readonly AppDbContext _appDbContext;
        public BunkerRepository(AppDbContext context) : base(context)
        {
            _appDbContext = context;
        }

        public override Task CreateOrUpdateAsync(Bunker entity)
        {
            if (entity.ScumServer is not null)
            {
                var tracked = _appDbContext.ChangeTracker.Entries<ScumServer>()
                    .FirstOrDefault(e => e.Entity.Id == entity.ScumServer.Id);

                if (tracked == null)
                {
                    _appDbContext.ScumServers.Attach(entity.ScumServer);
                }
                else
                {
                    entity.ScumServer = tracked.Entity;
                }
            }

            return base.CreateOrUpdateAsync(entity);
        }

        public Task<Bunker?> FindOneWithServerAsync(Expression<Func<Bunker, bool>> predicate)
        {
            return _appDbContext.Bunkers
                .Include(bunker => bunker.ScumServer)
                .FirstOrDefaultAsync(predicate);
        }
    }
}
