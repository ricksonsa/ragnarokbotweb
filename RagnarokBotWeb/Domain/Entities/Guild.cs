using Microsoft.EntityFrameworkCore;

namespace RagnarokBotWeb.Domain.Entities;

[Index(nameof(DiscordId), IsUnique = true)]
public class Guild : BaseEntity
{
    public bool RunTemplate { get; set; } = false;
    public ulong DiscordId { get; set; }
    public string DiscordName { get; set; }
    public bool Enabled { get; set; }
    public ScumServer ScumServer { get; set; }
}