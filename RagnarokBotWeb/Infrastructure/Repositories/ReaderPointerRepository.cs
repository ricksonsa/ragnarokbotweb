using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

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

    public override Task CreateOrUpdateAsync(ReaderPointer entity)
    {
        _dbContext.ScumServers.Attach(entity.ScumServer);
        return base.CreateOrUpdateAsync(entity);
    }
}