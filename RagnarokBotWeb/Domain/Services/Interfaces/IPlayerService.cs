using RagnarokBotWeb.Domain.Entities;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPlayerService
    {
        bool IsPlayerConnected(string steamId64);
        Task PlayerConnected(string steamId64, string scumId, string name);
        List<ScumPlayer> OnlinePlayers();
        Task<List<ScumPlayer>> OfflinePlayers();
        void ResetPlayersConnection();
        ScumPlayer? PlayerDisconnected(string steamId64);

        Task<Player> FindBySteamId64Async(string steamId);
        Task AddPlayerAsync(Player user);
        Task UpdatePlayerAsync(Player user);
        Task UpdatePlayerNameAsync(string steamId64, string name);
        Task UpdateFromScumPlayers(List<ScumPlayer>? players);
    }
}
