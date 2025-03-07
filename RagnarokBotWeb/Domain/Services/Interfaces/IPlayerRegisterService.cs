using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces;

public interface IPlayerRegisterService
{
    Task<PlayerRegister?> FindByGuildIdAndDiscordIdAsync(long guildId, ulong discordId);

    Task SaveAsync(PlayerRegister player);
}