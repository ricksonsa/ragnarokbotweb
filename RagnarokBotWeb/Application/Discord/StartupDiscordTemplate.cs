using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Dto;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class StartupDiscordTemplate(
    ILogger<StartupDiscordTemplate> logger,
    IChannelService channelService,
    IChannelTemplateService channelTemplateService,
    DiscordSocketClient client)
{
    public async Task Run(Guild guild)
    {
        if (guild.RunTemplate)
        {
            logger.LogWarning("Template already executed in GuildId: {GuildId} and DiscordId {DiscordId}", guild.Id, guild.DiscordId);
            return;
        }

        var discordCreateChannel = new DiscordCreateChannel(client, channelTemplateService);
        var channels = await discordCreateChannel.CreateAsync(guild.DiscordId);

        foreach (var channel in channels.Select(ToChannelEntity(guild)))
            await channelService.CreateChannelAsync(channel);
    }

    private static Func<ChannelDto, Channel> ToChannelEntity(Guild guild)
    {
        return dto => new Channel
        {
            Guild = guild,
            ChannelType = dto.ChannelType.ToString(),
            DiscordId = dto.DiscordId,
            Buttons = dto.Buttons.Select(x => new Button(x.Command, x.Label, x.MessageId)).ToList()
        };
    }
}