namespace RagnarokBotWeb.Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public decimal Amount { get; set; }
        public User User { get; set; }
    }
}
