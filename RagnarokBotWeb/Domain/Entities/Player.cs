namespace RagnarokBotWeb.Domain.Entities;

public class Player : BaseEntity
{
    public Player()
    {
        CreateDate = DateTime.Now;
    }

    public string? Name { get; set; }
    public string? ScumId { get; set; }
    public string? SteamId64 { get; set; }
    public string? SteamName { get; set; }
    public ulong? DiscordId { get; set; }
    public ScumServer ScumServer { get; set; }
    public long? Money { get; set; }
    public long? Gold { get; set; }
    public long? Fame { get; set; }
    public float? X { get; set; }
    public float? Y { get; set; }
    public float? Z { get; set; }
    public long Coin { get; set; } = 0;
    public DateTime CreateDate { get; set; }
}