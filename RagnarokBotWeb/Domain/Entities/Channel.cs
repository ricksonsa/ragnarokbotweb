using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities;

[Index(nameof(DiscordId), IsUnique = true)]
public class Channel : BaseEntity
{
    public Guild Guild { get; set; }
    public string? ChannelType { get; set; }
    public ulong DiscordId { get; set; }
    public List<Button>? Buttons { get; set; }

    public Channel()
    {
        Buttons = new List<Button>();
    }
}