using System.Net.Sockets;

namespace RagnarokBotWeb.Application.Models
{
    public class BotUser
    {
        public Guid Guid { get; set; }
        public string SteamId { get; set; }
        public DateTime LastInteracted { get; set; }
        public DateTime? LastCommand { get; set; }
        public DateTime? LastPinged { get; set; }
        public DateTime? LastReconnectSent { get; set; }
        [System.Text.Json.Serialization.JsonIgnore] public TcpClient? TcpClient { get; set; }
        public long ServerId { get; set; }

        public BotUser() { }

        public BotUser(Guid guid)
        {
            Guid = guid;
            LastInteracted = DateTime.UtcNow;
        }

        public override string ToString() => $"Guid[{Guid}] Server[{ServerId}] SteamId[{SteamId}] LastInteracted[{LastInteracted}] LastPinged[{LastPinged}]";

    }
}
