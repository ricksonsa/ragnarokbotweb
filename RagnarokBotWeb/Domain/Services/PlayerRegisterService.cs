using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class PlayerRegisterService(IPlayerRegisterRepository playerRegisterRepository) : IPlayerRegisterService
{
    public Task<PlayerRegister?> FindByGuildIdAndDiscordIdAsync(long guildId, ulong discordId)
    {
        return playerRegisterRepository.FindByGuildIdAndDiscordIdAsync(guildId, discordId);
    }

    public async Task SaveAsync(PlayerRegister player)
    {
        await playerRegisterRepository.CreateOrUpdateAsync(player);
        await playerRegisterRepository.SaveAsync();
    }
}