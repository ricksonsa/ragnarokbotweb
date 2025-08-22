using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class TaxiDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? DeliveryText { get; set; }
        public long? Price { get; set; } = 0;
        public long? VipPrice { get; set; } = 0;
        public string? ImageUrl { get; set; }
        public string? DiscordChannelId { get; set; }
        public ulong? DiscordMessageId { get; set; }
        public ETaxiType TaxiType { get; set; }
        public long? PurchaseCooldownSeconds { get; set; }
        public long? StockPerVipPlayer { get; set; }
        public long? MinPlayerOnline { get; set; }
        public long? StockPerPlayer { get; set; }
        public bool Enabled { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public bool IsVipOnly { get; set; }
        public long ScumServerId { get; set; }
        public List<TaxiTeleportDto> TaxiTeleports { get; set; }
    }
}
