using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class ReaderPointerRepository(AppDbContext appDbContext)
    : Repository<ReaderPointer>(appDbContext), IReaderPointerRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public override async Task<ReaderPointer?> FindOneAsync(Expression<Func<ReaderPointer, bool>> predicate)
    {
        return await _appDbContext.ReaderPointers
            .Include(x => x.ScumServer)
            .FirstOrDefaultAsync(predicate);
    }

    public override Task CreateOrUpdateAsync(ReaderPointer entity)
    {
        _appDbContext.Entry(entity.ScumServer).State = EntityState.Detached;
        _appDbContext.ScumServers.Attach(entity.ScumServer);
        return base.CreateOrUpdateAsync(entity);
    }
}