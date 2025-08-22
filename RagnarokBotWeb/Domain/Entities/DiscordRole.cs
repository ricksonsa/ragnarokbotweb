using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities
{
    public class DiscordRole : BaseEntity
    {
        public DateTime? ExpirationDate { get; set; }
        public required ulong DiscordId { get; set; }
        public bool Indefinitely { get; set; }
        public bool Processed { get; set; }

        public DiscordRole()
        {
            Indefinitely = true;
        }

        public DiscordRole(DateTime expirationDate)
        {
            ExpirationDate = expirationDate;
        }

        public bool IsExpired()
        {
            if (Indefinitely || !ExpirationDate.HasValue) return false;
            return ExpirationDate.Value.Date < DateTime.UtcNow.Date;
        }
    }
}
