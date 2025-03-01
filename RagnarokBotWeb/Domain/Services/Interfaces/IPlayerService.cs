using RagnarokBotWeb.Domain.Entities;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPlayerService
    {
        bool IsPlayerConnected(long serverId, string steamId64);
        Task PlayerConnected(Entities.ScumServer server, string steamId64, string scumId, string name);
        List<ScumPlayer> OnlinePlayers(long serverId);
        Task<List<ScumPlayer>> OfflinePlayers(long serverId);
        void ResetPlayersConnection(long serverId);
        ScumPlayer? PlayerDisconnected(long serverId, string steamId64);

        Task<Player?> FindBySteamId64Async(string steamId);
        Task AddPlayerAsync(Player user);
        Task UpdatePlayerAsync(Player user);
        Task UpdatePlayerNameAsync(string steamId64, string name);
        Task UpdateFromScumPlayers(long serverId, List<ScumPlayer>? players);
    }
}
