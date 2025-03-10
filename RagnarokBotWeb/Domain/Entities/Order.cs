using Shared.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Pack? Pack { get; set; }
        public EOrderStatus Status { get; set; }
        public Player? Player { get; set; }
        public ScumServer ScumServer { get; set; }

        public Order()
        {
            Status = EOrderStatus.Created;
        }
    }
}
