namespace RagnarokBotWeb.Application.BotServer
{
    // Supporting classes for state persistence
    public class BotServerState
    {
        public DateTime SavedAt { get; set; }
        public Dictionary<long, List<PersistedBotUser>> Servers { get; set; } = new();
    }
}