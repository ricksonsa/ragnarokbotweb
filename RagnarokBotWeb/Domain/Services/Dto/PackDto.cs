namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class PackDto
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long? Price { get; set; } = 0;
        public long? VipPrice { get; set; } = 0;
        public List<PackItemDto>? PackItems { get; set; }
        public List<string>? Commands { get; set; }
        public string? ImageUrl { get; set; }
        public string? DiscordChannelId { get; set; }
        public string? DiscordChannelName { get; set; }
        public long? PurchaseCooldownSeconds { get; set; } = 0;
        public long? StockPerVipPlayer { get; set; }
        public long? StockPerPlayer { get; set; } = 0;
        public bool? Enabled { get; set; }
        public bool? IsWelcomePack { get; set; }
        public bool? IsBlockPurchaseRaidTime { get; set; }
        public bool? IsVipOnly { get; set; }
        public string? DeliveryText { get; set; }
    }
}
