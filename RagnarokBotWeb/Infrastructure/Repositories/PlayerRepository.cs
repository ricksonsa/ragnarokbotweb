using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class PlayerRepository(AppDbContext appDbContext) : Repository<Player>(appDbContext), IPlayerRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public async Task<Player?> FindByGuildIdAndDiscordIdAsync(long guildId, ulong discordId)
    {
        return await _appDbContext.Players
            .Include(player => player.ScumServer)
            .Where(player => player.DiscordId == discordId)
            .Where(player => player.ScumServer.Guild.Id == guildId)
            .FirstOrDefaultAsync();
    }
}