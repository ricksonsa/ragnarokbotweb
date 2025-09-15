using RagnarokBotWeb.Domain.Entities.Base;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class CustomTask : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Cron { get; set; }
        public bool Enabled { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public bool DeleteExpired { get; set; }
        public string? StartMessage { get; set; }
        public long? MinPlayerOnline { get; set; }
        public DateTime? LastRunned { get; set; }
        public DateTime? ExpireAt { get; set; }
        public ECustomTaskType TaskType { get; set; }
        public ScumServer ScumServer { get; set; }
        public long? ScumServerId { get; set; }
        public string? Commands { get; set; }
    }
}
