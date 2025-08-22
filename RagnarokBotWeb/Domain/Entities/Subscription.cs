using RagnarokBotWeb.Domain.Entities.Base;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public int RollingDays { get; set; }
    }
}
