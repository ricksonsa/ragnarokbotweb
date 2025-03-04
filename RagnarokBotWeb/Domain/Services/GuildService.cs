using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class GuildService(IGuildRepository guildRepository) : IGuildService
{
    public Task<Guild?> FindByGuildIdAsync(long guildId)
    {
        return guildRepository.FindByIdAsync(guildId);
    }

    public async Task Update(Guild guild)
    {
        guildRepository.Update(guild);
        await guildRepository.SaveAsync();
    }

    public async Task<bool> IsActiveAsync(ulong discordId)
    {
        var guild = await guildRepository.FindOneAsync(x => x.DiscordId == discordId);
        if (guild == null) throw new GuildNotFoundException($"Guild with DiscordId: '{discordId}' not found.");
        return guild is { Enabled: true };
    }

    public Task ValidateGuildIsActiveAsync(ulong discordId)
    {
        if (IsActiveAsync(discordId).Result) return Task.CompletedTask;

        throw new GuildDisabledException($"Guild with DiscordId: '{discordId}' is disabled.");
    }
}