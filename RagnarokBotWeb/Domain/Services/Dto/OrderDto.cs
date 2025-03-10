using Shared.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class OrderDto
    {
        public PackDto? Pack { get; set; }
        public EOrderStatus Status { get; set; }
        public PlayerDto? Player { get; set; }
        public ScumServerDto ScumServer { get; set; }
    }
}
