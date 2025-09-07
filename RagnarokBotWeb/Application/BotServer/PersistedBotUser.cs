namespace RagnarokBotWeb.Application.BotServer
{
    public class PersistedBotUser
    {
        public Guid Guid { get; set; }
        public long ServerId { get; set; }
        public string? SteamId { get; set; }
        public DateTime? LastPinged { get; set; }
        public DateTime LastInteracted { get; set; }
        public DateTime? LastCommand { get; set; }
        public DateTime? LastReconnectSent { get; set; }
    }
}