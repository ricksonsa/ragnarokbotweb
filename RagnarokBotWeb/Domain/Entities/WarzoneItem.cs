namespace RagnarokBotWeb.Domain.Entities
{
    public class WarzoneItem : BaseEntity
    {
        public Item Item { get; set; }
        public long ItemId { get; set; }
        public Warzone Warzone { get; set; }
        public long WarzoneId { get; set; }
        public int Priority { get; set; } = 1;
        public DateTime? Deleted { get; set; }
    }
}
