using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Dto;
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
            logger.LogWarning("Template already executed in GuildId: {} and DiscordId {}", guild.Id, guild.DiscordId);
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

        var discordCreateChannel = new DiscordCreateChannel(client, serviceProvider);
        var channels = await discordCreateChannel.CreateAsync(guild.DiscordId);

        // FIXME: method temporally to delete channels before create new
        await DeleteChannelsAsync(channelService, guild);

        foreach (var channel in channels.Select(ToChannelEntity(guild)))
            await channelService.CreateChannelAsync(channel);
    }

    private static Func<ChannelDto, Channel> ToChannelEntity(Guild guild)
    {
        return dto => new Channel
        {
            Guild = guild,
            ChannelType = dto.ChannelType,
            DiscordId = dto.DiscordId,
            Buttons = dto.Buttons.Select(x => new Button
                {
                    Label = x.Label,
                    Command = x.Command
                }
            ).ToList()
        };
    }

    private async Task DeleteChannelsAsync(IChannelService channelService, Guild guild)
    {
        await channelService.DeleteAllAsync();
    }
}