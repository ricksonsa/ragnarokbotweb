using Shared.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class OrderDto
    {
        public long Id { get; set; }
        public PackDto? Pack { get; set; }
        public WarzoneDto? Warzone { get; set; }
        public TaxiDto? Taxi { get; set; }
        public EOrderType OrderType { get; set; }
        public EOrderStatus Status { get; set; }
        public PlayerDto? Player { get; set; }
        public ScumServerDto ScumServer { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
