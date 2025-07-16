namespace RagnarokBotWeb.Domain.Entities
{
    public class Silence : BaseEntity
    {
        public DateTime? ExpirationDate { get; set; }
        public bool Processed { get; set; }
    }
}
