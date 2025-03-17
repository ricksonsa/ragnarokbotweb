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
    public async Task Publish(ScumServer server, ChannelPublishDto dto, EChannelType channelType)
    {
        if (server.Guild == null)
        {
            logger.LogWarning("ScumServer = '{}' does not have a Guild yet.", server);
            return;
        }

        var guild = server.Guild!;

        using var scope = serviceProvider.CreateScope();
        var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

        var channel = await channelService.FindByGuildIdAndChannelTypeAsync(guild.Id, channelType);
        if (channel == null)
        {
            logger.LogWarning("Guild = '{}' does not have a channel with ChannelType = '{}'.", guild, channelType);
            return;
        }

        await DiscordSocketClientUtils.AwaitDiscordSocketClientIsReady(CancellationToken.None);

        var socketChannel = client.GetChannel(channel.DiscordId);
        if (socketChannel is not ISocketMessageChannel socketMessageChannel)
        {
            logger.LogWarning("SocketChannel = '{}' type not supported.", socketChannel.GetType().Name);
            return;
        }

        await socketMessageChannel.SendMessageAsync(dto.Content);
    }
}