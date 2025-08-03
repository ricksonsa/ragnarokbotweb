using RagnarokBotWeb.Application.Models;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Warzone : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string? DeliveryText { get; set; }
        public long Price { get; set; } = 0;
        public long VipPrice { get; set; } = 0;
        public string? ImageUrl { get; set; }
        public string? DiscordChannelId { get; set; }
        public ulong? DiscordMessageId { get; set; }
        public long? PurchaseCooldownSeconds { get; set; }
        public long WarzoneDurationInterval { get; set; } = 5;
        public long? MinPlayerOnline { get; set; }
        public long ItemSpawnInterval { get; set; }
        public long? StockPerPlayer { get; set; }
        public bool Enabled { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public bool IsVipOnly { get; set; }
        public ScumServer ScumServer { get; set; }
        public List<WarzoneItem> WarzoneItems { get; set; }
        public List<WarzoneTeleport> Teleports { get; set; }
        public List<WarzoneSpawn> SpawnPoints { get; set; }
        public DateTime? LastRunned { get; private set; }
        public DateTime? StopAt { get; private set; }
        public DateTime? Deleted { get; set; }
        public string? StartMessage { get; set; }
        public bool IsRunning
        {
            get
            {
                if (!StopAt.HasValue) return false;
                return DateTime.Now < StopAt.Value;
            }
        }

        public void Run()
        {
            StopAt = DateTime.Now.AddMinutes(WarzoneDurationInterval);
        }

        public void Stop()
        {
            StopAt = null;
            LastRunned = DateTime.Now;
        }

        public CreateEmbed WarzoneButtonEmbed()
        {
            var action = $"buy_warzone:{Id}";
            return new CreateEmbed
            {
                Buttons = [new($"Buy {Name} Teleport", action)],
                DiscordId = ulong.Parse(DiscordChannelId!),
                Text = Description,
                ImageUrl = ImageUrl,
                Title = Name
            };
        }

        public string ResolveDeliveryText(WarzoneItem warzoneItem, WarzoneSpawn warzoneSpawn)
        {
            if (DeliveryText is null) return "";
            return DeliveryText
                .Replace("{item_name}", warzoneItem.Item.Name)
                .Replace("{spawn_point_name}", warzoneSpawn.Teleport.Name);
        }
    }
}
