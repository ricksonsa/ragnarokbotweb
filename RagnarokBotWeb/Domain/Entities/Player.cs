namespace RagnarokBotWeb.Domain.Entities;
public class Player : BaseEntity
{
    public string? Name { get; set; }
    public string? ScumId { get; set; }
    public string? SteamId64 { get; set; }
    public string? SteamName { get; set; }
    public ulong? DiscordId { get; set; }
    public string? DiscordName { get; set; }
    public long ScumServerId { get; set; }           // FK
    public ScumServer ScumServer { get; set; }
    public bool HideAdminCommands { get; set; }
    public long? Money { get; set; }
    public long? Gold { get; set; }
    public long? Fame { get; set; }
    public float? X { get; set; }
    public float? Y { get; set; }
    public float? Z { get; set; }
    public long Coin { get; set; } = 0;
    public List<Vip> Vips { get; set; }
    public List<Ban> Bans { get; set; }
    public List<Silence> Silences { get; set; }

    public Player()
    {
        CreateDate = DateTime.UtcNow;
    }

    public bool IsVip() => Vips?.Any(vip => vip.ExpirationDate.HasValue && vip.ExpirationDate.Value.Date > DateTime.UtcNow.Date) ?? false;
    public bool IsSilenced() => Silences?.Any(silence => silence.ExpirationDate.HasValue && silence.ExpirationDate.Value.Date > DateTime.UtcNow.Date) ?? false;
    public bool IsBanned() => Bans?.Any(ban => ban.ExpirationDate.HasValue && ban.ExpirationDate.Value.Date > DateTime.UtcNow.Date) ?? false;
}