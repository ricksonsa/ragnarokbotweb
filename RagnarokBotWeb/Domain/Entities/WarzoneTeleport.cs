namespace RagnarokBotWeb.Domain.Entities
{
    public class WarzoneTeleport : BaseEntity
    {
        public required Warzone Warzone { get; set; }
        public required Teleport Teleport { get; set; }
    }
}
