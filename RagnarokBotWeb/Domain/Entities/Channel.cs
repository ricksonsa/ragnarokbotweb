using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities;

[Index(nameof(ChannelType), IsUnique = true)]
[Index(nameof(DiscordId), IsUnique = true)]
public class Channel : BaseEntity
{
    public Guild Guild { get; set; }
    public EChannelType ChannelType { get; set; }
    public ulong DiscordId { get; set; }
    public List<Button>? Buttons { get; set; }
}