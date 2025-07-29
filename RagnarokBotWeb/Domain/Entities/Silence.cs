namespace RagnarokBotWeb.Domain.Entities
{
    public class Silence : BaseEntity
    {
        public DateTime? ExpirationDate { get; set; }
        public bool Indefinitely { get; set; }
        public bool Processed { get; set; }

        public Silence()
        {
            Indefinitely = true;
        }

        public Silence(DateTime expirationDate)
        {
            ExpirationDate = expirationDate;
        }
    }
}
