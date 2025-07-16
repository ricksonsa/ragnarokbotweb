using Shared.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Bot : BaseEntity
    {
        public bool Active { get; set; }
        public EBotState State { get; set; } = EBotState.None;
        public DateTime? LastInteracted { get; set; }
        public ScumServer ScumServer { get; set; }

        public Bot(ScumServer server)
        {
            ScumServer = server;
            State = EBotState.Offline;
        }

        public Bot() { }

        public void UpdateInteraction()
        {
            LastInteracted = DateTime.UtcNow;
        }
    }
}