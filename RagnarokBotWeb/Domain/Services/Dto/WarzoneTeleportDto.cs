namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class WarzoneTeleportDto
    {
        public long Id { get; set; }
        public long WarzoneId { get; set; }
        public TeleportDto Teleport { get; set; }
        public long TeleportId { get; set; }
    }
}
