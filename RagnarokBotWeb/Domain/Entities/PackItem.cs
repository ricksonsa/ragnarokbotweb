namespace RagnarokBotWeb.Domain.Entities
{
    public class PackItem : BaseEntity
    {
        public Item Item { get; set; }
        public Pack Pack { get; set; }
        public int Amount { get; set; } = 1;
        public int AmmoCount { get; set; }
    }
}
