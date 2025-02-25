using Discord.WebSocket;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class StartupDiscordTemplate(
    ILogger<StartupDiscordTemplate> logger,
    IServiceProvider serviceProvider,
    DiscordSocketClient client)
{
    public async Task Run(Guild guild)
    {
        if (guild.RunTemplate)
        {
            logger.LogWarning(
                $"Template already executed in GuildId: {guild.Id} and External GuildId {guild.DiscordId}");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

        var channels = await new DiscordCreateChannel(client, serviceProvider).CreateAsync(guild.DiscordId);

        foreach (var channel in channels.Select(dto => new Channel
                 {
                     Guild = guild,
                     ChannelType = dto.ChannelType,
                     DiscordId = dto.DiscordId
                 }))
            await channelService.CreateChannelAsync(channel);
    }
}