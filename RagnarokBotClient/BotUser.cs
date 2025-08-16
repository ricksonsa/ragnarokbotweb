namespace RagnarokBotClient
{
    public class BotUser
    {
        public string Guid { get; set; }
        public DateTime? LastPinged { get; set; }
        public bool NeedsReconnect
        {
            get
            {
                var diff = (DateTime.UtcNow - LastPinged!.Value).TotalMinutes;
                return diff >= 5;
            }
        }
    }
}
