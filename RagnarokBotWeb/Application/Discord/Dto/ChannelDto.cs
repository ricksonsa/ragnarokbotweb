using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Application.Discord.Dto;

public class ChannelDto(ulong discordId, ChannelTemplateValue channelType, List<ButtonDto>? buttons = null)
{
    public ulong DiscordId { get; } = discordId;
    public ChannelTemplateValue ChannelType { get; } = channelType;
    public List<ButtonDto> Buttons { get; } = buttons ?? [];
}