using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IPlayerRepository : IRepository<Player>
    {
        Task<Player?> FindByGuildIdAndDiscordIdAsync(long guildId, ulong discordId);
    }
}
