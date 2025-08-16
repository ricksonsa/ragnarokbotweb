using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class CustomTaskDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool Enabled { get; set; }
        public string Cron { get; set; }
        public bool IsBlockPurchaseRaidTime { get; set; }
        public string? StartMessage { get; set; }
        public long? MinPlayerOnline { get; set; }
        public DateTime? LastRunned { get; set; }
        public ECustomTaskType TaskType { get; set; }
        public long? ScumServerId { get; set; }
        public string? Commands { get; set; }
    }
}
