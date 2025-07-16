using Shared.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class BotDto
    {
        public bool Active { get; set; }
        public EBotState State { get; set; } = EBotState.None;
        public DateTime? LastInteracted { get; set; }
        public ScumServerDto ScumServer { get; set; }

    }
}
