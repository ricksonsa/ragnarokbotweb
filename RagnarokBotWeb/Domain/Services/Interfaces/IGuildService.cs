using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces;

public interface IGuildService
{
    Task<Guild?> FindByGuildIdAsync(long guildId);
    
    Task Update(Guild guild);
}