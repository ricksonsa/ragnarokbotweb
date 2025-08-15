namespace RagnarokBotWeb.Application.Models
{
    public class BotUser
    {
        public Guid Guid { get; set; }
        public string SteamId { get; set; }
        public DateTime LastInteracted { get; set; }
        public DateTime? LastPinged { get; set; }

        public BotUser() { }

        public BotUser(Guid guid)
        {
            Guid = guid;
            LastInteracted = DateTime.UtcNow;
        }

        public override string ToString() => $"Guid[{Guid}] SteamId[{SteamId}] LastInteracted[{LastInteracted}] LastPinged[{LastPinged}]";

    }
}
