using Shared.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Pack? Pack { get; set; }
        public Warzone? Warzone { get; set; }
        public EOrderStatus Status { get; set; }
        public EOrderType OrderType { get; set; }

        public Player? Player { get; set; }
        public ScumServer ScumServer { get; set; }

        public Order()
        {
            Status = EOrderStatus.Created;
        }

        public string ResolvedDeliveryText()
        {
            if (Pack == null) return null;

            return Pack.DeliveryText
                .Replace("{playerName}", Player?.Name)
                .Replace("{packageName}", Pack?.Name)
                .Replace("{orderId}", Id.ToString());
        }
    }
}
