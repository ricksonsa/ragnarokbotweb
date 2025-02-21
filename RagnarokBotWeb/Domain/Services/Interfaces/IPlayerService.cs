using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPlayerService
    {
        bool IsPlayerConnected(string steamId64);
        Task PlayerConnected(string steamId64, string scumId, string name);
        List<Player> OnlinePlayers();
        Task<List<Player>> OfflinePlayers();
        void ResetPlayersConnection();
        Player? PlayerDisconnected(string steamId64);
    }
}
