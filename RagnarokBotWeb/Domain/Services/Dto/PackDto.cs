namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class PackDto
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; } = decimal.Zero;
        public decimal? VipPrice { get; set; } = decimal.Zero;
        public List<PackItemDto>? PackItems { get; set; }
        public List<string>? Commands { get; set; }
        public string? ImageUrl { get; set; }
        public string? DiscordChannelId { get; set; }
        public string? DiscordChannelName { get; set; }
        public long? PurchaseCooldownSeconds { get; set; } = 0;
        public long? StockPerPlayer { get; set; } = 0;
        public bool? Enabled { get; set; }
        public bool? IsWelcomePack { get; set; }
        public bool? IsBlockPurchaseRaidTime { get; set; }
        public bool? IsVipOnly { get; set; }
        public string? DeliveryText { get; set; }
    }
}
