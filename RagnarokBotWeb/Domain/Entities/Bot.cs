using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Bot : BaseEntity
    {
        public string SteamId64 { get; set; }
        public bool Active { get; set; }
        public EBotState State { get; set; } = EBotState.None;
    }
}
