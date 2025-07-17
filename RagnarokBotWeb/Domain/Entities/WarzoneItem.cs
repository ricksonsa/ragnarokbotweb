namespace RagnarokBotWeb.Domain.Entities
{
    public class WarzoneItem : BaseEntity
    {
        public required Item Item { get; set; }
        public required Warzone Warzone { get; set; }
        public int Priority { get; set; } = 1;
        public DateTime? Deleted { get; set; }
    }
}
