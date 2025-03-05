using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories;

public class PlayerRegisterRepository(AppDbContext appDbContext)
    : Repository<PlayerRegister>(appDbContext), IPlayerRegisterRepository
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public async Task<PlayerRegister?> FindByGuildIdAndDiscordIdAsync(long guildId, ulong discordId)
    {
        return await _appDbContext.PlayerRegisters
            .Include(player => player.ScumServer)
            .Where(player => player.DiscordId == discordId)
            .Where(player => player.ScumServer.Guild.Id == guildId)
            .FirstOrDefaultAsync();
    }
}