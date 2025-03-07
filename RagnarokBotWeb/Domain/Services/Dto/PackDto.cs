namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class PackDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal VipPrice { get; set; }
        public List<ItemToPackDto> Items { get; set; }
        public List<string> Commands { get; set; }
        public string? ImageUrl { get; set; }
        public string? DiscordChannelId { get; set; }
        public string? DiscordChannelName { get; set; }
        public long PurchaseCooldownSeconds { get; set; }
        public long StockPerPlayer { get; set; }
        public bool Enabled { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public bool IsVipOnly { get; set; }
        public bool IsDailyPackage { get; set; }
        public string? DeliveryText { get; set; }
    }

    public class ItemToPackDto()
    {
        public long ItemId { get; set; }
        public long PackId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemCode { get; set; }
        public int Amount { get; set; }
    }
}
