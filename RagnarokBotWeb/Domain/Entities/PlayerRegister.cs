using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities;

public class PlayerRegister : BaseEntity
{
    public string WelcomePackId { get; set; }
    public ulong DiscordId { get; set; }
    public ScumServer ScumServer { get; set; }
    public EPlayerRegisterStatus Status { get; set; }
}