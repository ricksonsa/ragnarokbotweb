namespace RagnarokBotWeb.Domain.Entities
{
    public class WarzoneSpawn : BaseEntity
    {
        public Warzone Warzone { get; set; }
        public long WarzoneId { get; set; }
        public Teleport Teleport { get; set; }
        public long TeleportId { get; set; }
    }
}
