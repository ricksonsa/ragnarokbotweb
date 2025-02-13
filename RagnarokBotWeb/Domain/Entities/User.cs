namespace RagnarokBotWeb.Domain.Entities
{
    public class User : BaseEntity
    {
        public string? Name { get; set; }
        public string? SteamId64 { get; set; }
        public string? ScumId { get; set; }
        public string? DiscordId { get; set; }
        public string? Presence { get; set; }
        public decimal Balance { get; set; } = 0;
        public DateTime CreateDate { get; set; }
    }
}
