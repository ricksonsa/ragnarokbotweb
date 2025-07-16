namespace RagnarokBotWeb.Domain.Entities
{
    public class Vip : BaseEntity
    {
        public DateTime? ExpirationDate { get; set; }
        public bool Processed { get; set; }
    }
}
