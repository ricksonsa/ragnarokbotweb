namespace RagnarokBotWeb.Domain.Entities
{
    public class User : BaseEntity
    {
        public string? Name { get; set; }
        public string? SteamId64 { get; set; }
        public string? ScumId { get; set; }
        public string? DiscordId { get; set; }
        public string? Presence { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
