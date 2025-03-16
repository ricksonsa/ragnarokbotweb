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

    public override Task AddRangeAsync(IList<Reader> entities)
    {
        foreach (var entity in entities)
        {
            var existingServer = _dbContext.ScumServers.Local.FirstOrDefault(s => s.Id == entity.ScumServer.Id);

            if (existingServer != null)
            {
                entity.ScumServer = existingServer;
            }
            else
            {
                _dbContext.Attach(entity.ScumServer);
            }
        }
        
        return base.AddRangeAsync(entities);
    }
}