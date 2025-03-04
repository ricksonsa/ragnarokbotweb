using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Application.Discord.Dto;

public class ChannelDto(ulong discordId, EChannelType channelType, List<ButtonDto>? buttons = null)
{
    public ulong DiscordId { get; } = discordId;
    public EChannelType ChannelType { get; } = channelType;
    public List<ButtonDto> Buttons { get; } = buttons ?? [];
}