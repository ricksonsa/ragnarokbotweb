namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class WarzoneSpawnDto
    {
        public long Id { get; set; }
        public long WarzoneId { get; set; }
        public TeleportDto Teleport { get; set; }
    }
}
