using Shared.Enums;

namespace RagnarokBotWeb.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Pack? Pack { get; set; }
        public EOrderStatus Status { get; set; }
        public User? User { get; set; }
        public DateTime CreateDate { get; set; }

        public Order()
        {
            CreateDate = DateTime.Now;
            Status = EOrderStatus.Created;
        }
    }
}
