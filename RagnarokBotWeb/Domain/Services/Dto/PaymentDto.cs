

using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class PaymentDto
    {
        public long Id { get; set; }
        public SubscriptionDto Subscription { get; set; }
        public DateTime? ConfirmDate { get; set; }
        public EPaymentStatus Status { get; set; }
        public DateTime? ExpireAt { get; set; }
        public bool IsExpired { get; set; }
        public string? Url { get; set; }

    }
}
