namespace RagnarokBotWeb.Domain.Entities
{
    public class Ban : BaseEntity
    {
        public DateTime? ExpirationDate { get; set; }
        public bool Indefinitely { get; set; }
        public bool Processed { get; set; }

        public Ban()
        {
            Indefinitely = true;
        }

        public Ban(DateTime expirationDate)
        {
            ExpirationDate = expirationDate;
        }
    }
}
