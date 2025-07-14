namespace RagnarokBotWeb.Application.Models
{
    public class ChatTextParseResult
    {
        public string SteamId { get; set; }
        public string Text { get; set; }
        public string PlayerName { get; set; }
        public string ChatType { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
