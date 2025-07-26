using Microsoft.EntityFrameworkCore;
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

    public override Task CreateOrUpdateAsync(Channel entity)
    {
        if (entity.Guild != null) _context.Guilds.Attach(entity.Guild);
        return base.CreateOrUpdateAsync(entity);
    }

    public Task<Channel?> FindOneByServerIdAndChatType(long serverId, string chatType)
    {
        return DbSet()
            .Include(channel => channel.ChannelType)
            .Include(channel => channel.Guild)
            .Include(channel => channel.Guild.ScumServer)
            .FirstOrDefaultAsync(channel => channel.Guild.ScumServer.Id == serverId && channel.ChannelType == chatType);
    }
}