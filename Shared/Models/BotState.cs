namespace Shared.Models
{
    public class BotState
    {
        public string BotId { get; set; }
        public string SteamId { get; set; }
        public bool Connected { get; set; }
        public bool GameActive { get; set; }
        public string LastSeen { get; set; }
        public double MinutesSinceLastSeen { get; set; }
    }
}
