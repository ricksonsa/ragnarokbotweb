namespace RagnarokBotWeb.Application.BotServer
{
    // Supporting classes for enhanced bot statistics
    public class BotServerStats
    {
        public long ServerId { get; set; }
        public int TotalBots { get; set; }
        public int ConnectedBots { get; set; }
        public int ActiveBots { get; set; }
        public List<BotStatus> Bots { get; set; } = new();
    }
}