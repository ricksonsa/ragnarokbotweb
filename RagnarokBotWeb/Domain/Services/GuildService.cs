using Discord.WebSocket;
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

    public Task<Guild?> FindByServerIdAsync(long serverId)
    {
        return guildRepository.FindByServerIdAsync(serverId);
    }

    public async Task<Guild> FindByDiscordIdAsync(ulong discordId)
    {
        var guild = await guildRepository.FindOneWithScumServerAsync(guild => guild.DiscordId == discordId);
        if (guild is null) throw new GuildNotFoundException(discordId);
        return guild;
    }

    public async Task Update(Guild guild)
    {
        guildRepository.Update(guild);
        await guildRepository.SaveAsync();
    }

    public async Task<bool> IsActiveAsync(ulong discordId)
    {
        var guild = await guildRepository.FindOneAsync(x => x.DiscordId == discordId);
        if (guild == null) throw new GuildNotFoundException(discordId);
        return guild is { Enabled: true };
    }

    public async Task ValidateGuildIsActiveAsync(ulong discordId)
    {
        if (await IsActiveAsync(discordId)) return;

        throw new GuildDisabledException($"Guild with DiscordId: '{discordId}' is disabled.");
    }

    public async Task<Guild> CreateGuildIfNotExistent(SocketGuild socketGuild)
    {
        var guild = await guildRepository.FindOneAsync(g => g.DiscordId == socketGuild.Id);
        if (guild is null)
        {
            guild = new Guild
            {
                DiscordId = socketGuild.Id,
                DiscordName = socketGuild.Name,
                Token = $"{Guid.NewGuid()}-{socketGuild.Id}" // FIXME: Resolver esse debito de codigo
            };
            await guildRepository.CreateOrUpdateAsync(guild);
            await guildRepository.SaveAsync();
        }
        return guild;
    }
}