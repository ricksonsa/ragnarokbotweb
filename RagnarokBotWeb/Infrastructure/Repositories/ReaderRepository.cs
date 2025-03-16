using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class ReaderRepository : Repository<Reader>, IReaderRepository
{
    private readonly AppDbContext _dbContext;

    public ReaderRepository(AppDbContext context) : base(context)
    {
        _dbContext = context;
    }

    public override Task AddAsync(Reader entity)
    {
        _dbContext.Entry(entity.ScumServer).State = EntityState.Detached;
        _dbContext.ScumServers.Attach(entity.ScumServer);
        return base.AddAsync(entity);
    }
}