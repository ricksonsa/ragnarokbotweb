using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class ChannelRepository(AppDbContext context) : Repository<Channel>(context), IChannelRepository
{
    private readonly AppDbContext _context = context;

    public override Task AddAsync(Channel entity)
    {
        _context.Guilds.Attach(entity.Guild);
        return base.AddAsync(entity);
    }
}