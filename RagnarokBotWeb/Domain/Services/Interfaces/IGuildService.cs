using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces;

public interface IGuildService
{
    Task<Guild?> FindByGuildIdAsync(long guildId);
    Task<Guild> FindByDiscordIdAsync(ulong guildId);

    Task Update(Guild guild);

    Task<bool> IsActiveAsync(ulong discordId);

    Task ValidateGuildIsActiveAsync(ulong discordId);
}