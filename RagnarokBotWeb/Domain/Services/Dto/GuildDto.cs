namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class GuildDto
    {
        public ulong DiscordId { get; set; }
        public string DiscordName { get; set; }
        public string Token { get; set; }
        public bool Confirmed { get; set; }
        public bool Enabled { get; set; }
        public bool RunTemplate { get; set; }
        public long ServerId { get; set; }
        public string? DiscordLink { get; internal set; }
        public List<ChannelDto> Channels { get; set; }
    }
}
