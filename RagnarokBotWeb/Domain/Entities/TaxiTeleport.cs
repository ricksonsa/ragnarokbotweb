using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities
{
    public class TaxiTeleport : BaseEntity
    {
        public Taxi Taxi { get; set; }
        public long TaxiId { get; set; }
        public Teleport Teleport { get; set; }
        public long TeleportId { get; set; }
    }
}
