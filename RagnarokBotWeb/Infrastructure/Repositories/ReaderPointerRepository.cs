using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class ReaderPointerRepository(AppDbContext dbContext)
    : Repository<ReaderPointer>(dbContext), IReaderPointerRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public override async Task<ReaderPointer?> FindOneAsync(Expression<Func<ReaderPointer, bool>> predicate)
    {
        return await _dbContext.ReaderPointers
            .Include(x => x.ScumServer)
            .FirstOrDefaultAsync(predicate);
    }

    public override async Task CreateOrUpdateAsync(ReaderPointer entity)
    {
        if (entity.ScumServer is not null)
        {
            var tracked = _dbContext.ChangeTracker.Entries<ScumServer>()
                .FirstOrDefault(e => e.Entity.Id == entity.ScumServer.Id);

            if (tracked == null)
            {
                _dbContext.ScumServers.Attach(entity.ScumServer);
            }
            else
            {
                entity.ScumServer = tracked.Entity;
            }
        }

        await base.CreateOrUpdateAsync(entity);
    }
}