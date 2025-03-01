using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Application.Discord;

public class ChannelDto(ulong discordId, EChannelType channelType)
{
    public ulong DiscordId { get; } = discordId;
    public EChannelType ChannelType { get; } = channelType;
}