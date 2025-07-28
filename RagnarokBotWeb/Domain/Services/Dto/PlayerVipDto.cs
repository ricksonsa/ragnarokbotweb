namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class PlayerVipDto
    {
        public int? Days { get; set; }
        public bool Whitelist { get; set; }
        public ulong? DiscordRoleId { get; set; }
    }
}
