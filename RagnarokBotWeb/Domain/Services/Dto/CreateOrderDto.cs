namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class CreateOrderDto
    {
        public string? DiscordId { get; set; }
        public string? SteamId { get; set; }
        public long PackId { get; set; }
    }
}
