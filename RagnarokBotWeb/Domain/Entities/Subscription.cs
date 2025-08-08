namespace RagnarokBotWeb.Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public int RollingDays { get; set; }
        public bool IsExpired()
        {
            var now = DateTime.UtcNow;
            return false;
        }
    }
}
