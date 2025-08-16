using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Domain.Services;

public class DiscordChannelPublisher(
    DiscordSocketClient client,
    ILogger<DiscordChannelPublisher> logger,
    IServiceProvider serviceProvider)
{
    public async Task Publish(ScumServer server, ChannelPublishDto dto, ChannelTemplateValue channelType)
    {
        if (server.Guild == null)
        {
            logger.LogDebug("ScumServer = '{Id}' does not have a Guild yet.", server.Id);
            return;
        }

        var guild = server.Guild!;

        using var scope = serviceProvider.CreateScope();
        var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

        var channel = await channelService.FindByGuildIdAndChannelTypeAsync(guild.Id, channelType);
        if (channel == null)
        {
            logger.LogDebug("Guild = '{GuildId}' does not have a channel with ChannelType = '{Type}'.", guild.Id, channelType);
            return;
        }

        await DiscordSocketClientUtils.AwaitDiscordSocketClientIsReady(CancellationToken.None);

        var socketChannel = client.GetChannel(channel.DiscordId);
        if (socketChannel is not ISocketMessageChannel socketMessageChannel)
        {
            logger.LogDebug("SocketChannel = '{Name}' type not supported.", socketChannel.GetType().Name);
            return;
        }

        await socketMessageChannel.SendMessageAsync(dto.Content);
    }
}