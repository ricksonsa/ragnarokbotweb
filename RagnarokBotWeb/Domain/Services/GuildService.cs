using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class GuildService(IGuildRepository guildRepository) : IGuildService
{
    public Task<Guild> FindByGuildIdAsync(long guildId)
    {
        return guildRepository.GetByIdAsync(guildId);
    }

    public async Task Update(Guild guild)
    {
        guildRepository.Update(guild);
        await guildRepository.SaveAsync();
    }
}