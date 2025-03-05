using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

public interface IPlayerRegisterRepository : IRepository<PlayerRegister>
{
    Task<PlayerRegister?> FindByGuildIdAndDiscordIdAsync(long guildId, ulong discordId);
}