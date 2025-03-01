namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class ChangeGuildDto
    {
        public ulong GuildId { get; set; }
        public bool RunTemplate { get; set; } = true;
    }
}
