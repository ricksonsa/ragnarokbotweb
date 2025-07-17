namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class WarzoneDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? DeliveryText { get; set; }
        public long? Price { get; set; } = 0;
        public long? VipPrice { get; set; } = 0;
        public string? ImageUrl { get; set; }
        public string? DiscordChannelId { get; set; }
        public ulong? DiscordMessageId { get; set; }
        public long? PurchaseCooldownSeconds { get; set; }
        public long WarzoneDurationInterval { get; set; } = 5;
        public long? MinPlayerOnline { get; set; }
        public long? ItemSpawnInterval { get; set; }
        public long? StockPerPlayer { get; set; }
        public bool Enabled { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public bool IsVipOnly { get; set; }
        public long ScumServerId { get; set; }
        public List<WarzoneItemDto> WarzoneItems { get; set; }
        public List<WarzoneTeleportDto> Teleports { get; set; }
        public List<WarzoneSpawnDto> SpawnPoints { get; set; }
        public DateTime? Deleted { get; set; }
        public string? StartMessage { get; set; }
    }
}
