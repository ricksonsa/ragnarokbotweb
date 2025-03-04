using RagnarokBotWeb.Domain.Entities;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPlayerService
    {
        bool IsPlayerConnected(string steamId64, long? serverId = null);
        Task PlayerConnected(Entities.ScumServer server, string steamId64, string scumId, string name);
        List<ScumPlayer> OnlinePlayers(long serverId);
        Task<List<ScumPlayer>> OfflinePlayers(long serverId);
        void ResetPlayersConnection(long? serverId = null);
        ScumPlayer? PlayerDisconnected(long serverId, string steamId64);

        // FIXME: pass guild id, because user can be in multiples guilds with same steam id
        Task<Player?> FindBySteamId64Async(string steamId);
        Task<Player?> FindByGuildIdAndDiscordIdAsync(long guildId, ulong discordId);
        Task AddPlayerAsync(Player user);
        Task UpdatePlayerAsync(Player user);
        Task UpdatePlayerNameAsync(string steamId64, string name);
        Task UpdateFromScumPlayers(long serverId, List<ScumPlayer>? players);
    }
}
