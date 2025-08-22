namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class TaxiTeleportDto
    {
        public long Id { get; set; }
        public long TaxiId { get; set; }
        public TeleportDto Teleport { get; set; }
        public long TeleportId { get; set; }
    }
}
