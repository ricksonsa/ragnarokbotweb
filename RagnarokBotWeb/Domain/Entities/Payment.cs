using RagnarokBotWeb.Domain.Entities.Base;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public long TenantId { get; set; }
        public Tenant Tenant { get; set; }
        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }
        public EPaymentStatus Status { get; set; } = EPaymentStatus.Waiting;
        public DateTime? ConfirmDate { get; set; }
        public string? Url { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime? ExpireAt { get; set; }

        public bool IsActive()
        {
            if (!ConfirmDate.HasValue) return false;

            var now = DateTime.UtcNow;
            var expirationDate = ConfirmDate.Value.AddDays(Subscription.RollingDays);
            return now < expirationDate;
        }
    }
}
