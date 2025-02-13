namespace RagnarokBotWeb.Domain.Entities
{
    public class Pack : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; } = 0;
        public decimal VipPrice { get; set; } = 0;
        public List<PackItem> PackItems { get; set; }
    }
}
