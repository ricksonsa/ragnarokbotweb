using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public decimal Amount { get; set; }
        public Player User { get; set; }
    }
}
