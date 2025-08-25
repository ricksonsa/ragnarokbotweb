namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class UavDto
    {
        public long Id { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; } = "🛰️ UAV Scan Report";
        public string? Description { get; set; } = "Real-time reconnaissance data from an unmanned aerial vehicle. Use this intelligence to track movement, locate targets, or prepare for engagement.";
        public string? DeliveryText { get; set; } = "UAV is now scanning sector {sector}";
        public string? DiscordChannelId { get; set; }
        public ulong? DiscordMessageId { get; set; }
        public long Price { get; set; } = 0;
        public long VipPrice { get; set; } = 0;
        public long? PurchaseCooldownSeconds { get; set; }
        public long? StockPerPlayer { get; set; }
        public long? StockPerVipPlayer { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public bool SendToUserDM { get; set; }

        public bool IsVipOnly { get; set; }
        public string? ImageUrl { get; set; }
    }
}
