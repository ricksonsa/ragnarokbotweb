using Shared.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Bot : BaseEntity
    {
        public string Identifier { get; set; }
        public bool Active { get; set; }
        public EBotState State { get; set; } = EBotState.None;
        public DateTime? LastInteracted { get; set; }

        public void UpdateInteraction()
        {
            LastInteracted = DateTime.Now;
        }
    }
}
