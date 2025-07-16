namespace RagnarokBotWeb.Domain.Entities
{
    public class Ban : BaseEntity
    {
        public DateTime? ExpirationDate { get; set; }
        public bool Processed { get; set; }
    }
}
