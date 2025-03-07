using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class ChannelTemplateRepository(AppDbContext appDbContext)
    : Repository<ChannelTemplate>(appDbContext), IChannelTemplateRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public override async Task<IEnumerable<ChannelTemplate>> GetAllAsync()
    {
        return await _appDbContext.ChannelTemplates
            .Include(channel => channel.Buttons)
            .ToListAsync();
    }
}