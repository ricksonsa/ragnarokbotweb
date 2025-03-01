namespace RagnarokBotWeb.Domain.Entities
{
    public class Guild : BaseEntity
    {
        public bool RunTemplate { get; set; } = false;
        public ulong DiscordId { get; set; }
        public bool Enabled { get; set; }
    }
}
