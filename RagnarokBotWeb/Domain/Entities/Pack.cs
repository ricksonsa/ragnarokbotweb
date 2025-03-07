namespace RagnarokBotWeb.Domain.Entities
{
    public class Pack : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; } = 0;
        public decimal VipPrice { get; set; } = 0;
        public string? Commands { get; set; }
        public string? ImageUrl { get; set; }
        public string? DiscordChannelId { get; set; }
        public long? PurchaseCooldownSeconds { get; set; }
        public long? StockPerPlayer { get; set; }
        public bool Enabled { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public bool IsVipOnly { get; set; }
        public bool IsDailyPackage { get; set; }
        public ScumServer ScumServer { get; set; }

        public List<PackItem> PackItems { get; set; }
    }
}
