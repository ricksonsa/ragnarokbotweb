namespace RagnarokBotWeb.Domain.Entities
{
    public class Vip : BaseEntity
    {
        public DateTime? ExpirationDate { get; set; }
        public bool Indefinitely { get; set; }

        public Vip()
        {
            Indefinitely = true;
        }

        public Vip(DateTime expirationDate)
        {
            ExpirationDate = expirationDate;
        }

    }
}
