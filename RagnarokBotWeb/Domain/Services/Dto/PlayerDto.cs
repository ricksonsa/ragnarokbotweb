namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class PlayerDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? ScumId { get; set; }
        public string? SteamId64 { get; set; }
        public string? SteamName { get; set; }
        public string? DiscordId { get; set; }
        public string? DiscordName { get; set; }
        public ScumServerDto ScumServer { get; set; }
        public long? Money { get; set; }
        public long? Gold { get; set; }
        public long? Fame { get; set; }
        public long Coin { get; set; } = 0;
        public DateTime CreateDate { get; set; }
    }
}
