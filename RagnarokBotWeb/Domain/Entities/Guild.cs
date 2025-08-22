using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities;

[Index(nameof(DiscordId), IsUnique = true)]
public class Guild : BaseEntity
{
    public bool RunTemplate { get; set; } = false;
    public ulong DiscordId { get; set; }
    public string DiscordName { get; set; }
    public string Token { get; set; }
    public bool Confirmed { get; set; }
    public bool Enabled { get; set; }
    public ScumServer ScumServer { get; set; }
    public string? DiscordLink { get; internal set; }
    public List<Channel> Channels { get; set; }

    public Guild() { }

    public Guild(ulong discordId)
    {
        DiscordId = discordId;
        Token = $"{Guid.NewGuid()}-{discordId}";
    }
}