namespace RagnarokBotWeb.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Tenant Tenant { get; set; }
        public Subscription Subscription { get; set; }
        public bool Confirmed { get; set; }
    }
}
