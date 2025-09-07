namespace RagnarokBotWeb.Application.BotServer
{
    public class BotStatus
    {
        public Guid Guid { get; set; }
        public string SteamId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastPinged { get; set; }
        public DateTime LastInteracted { get; set; }
        public DateTime? LastCommand { get; set; }
        public DateTime? LastReconnectSent { get; set; }
        public double? MinutesSinceLastPing { get; set; }
        public double MinutesSinceLastInteraction { get; set; }
        public bool RestoredFromState { get; set; }
    }
}