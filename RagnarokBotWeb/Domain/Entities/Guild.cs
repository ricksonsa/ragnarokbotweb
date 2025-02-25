namespace RagnarokBotWeb.Domain.Entities;

public class Guild : BaseEntity
{
    public bool RunTemplate { get; set; } = false;
    public Tenant Tenant { get; set; }
    public bool Enabled { get; set; }
    public ulong DiscordId { get; set; }
}