using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;
using Shared.Models;
using static RagnarokBotWeb.Application.Tasks.Jobs.KillRankJob;
using static RagnarokBotWeb.Application.Tasks.Jobs.LockpickRankJob;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPlayerService
    {
        bool IsPlayerConnected(string steamId64, long? serverId = null);
        Task PlayerConnected(Entities.ScumServer server, string steamId64, string scumId, string name, float x, float y, float z, string ipAddress);
        List<ScumPlayer> OnlinePlayers(long serverId);
        Task<List<ScumPlayer>> OfflinePlayers(long serverId);
        void ResetPlayersConnection(long? serverId = null);
        ScumPlayer? PlayerDisconnected(long serverId, string steamId64);
        Task<List<GrapthDto>> NewPlayersPerMonth();
        Task<List<PlayerStatsDto>> KillRank();
        Task<List<PlayerStatsDto>> KillRank(string steamId);
        Task<List<LockpickStatsDto>> LockpickRank();
        Task<List<LockpickStatsDto>> LockpickRank(string steamId);

        Task<Player?> FindBySteamId64Async(string steamId, long serverId);
        Task AddPlayerAsync(Player user);
        Task UpdatePlayerAsync(Player user);
        Task UpdatePlayerNameAsync(Entities.ScumServer server, string steamId64, string scumId, string name);

        Task<Page<PlayerDto>> GetPlayers(Paginator paginator, string? filter);
        Task<Page<PlayerDto>> GetVipPlayers(Paginator paginator, string? filter);
        Task<PlayerDto> GetPlayer(long id);
        Task<PlayerDto> GetPlayerBySteamId(string id);
        Task<PlayerDto> RemoveVip(long id);
        Task<PlayerDto> RemoveDiscordRole(long id, ulong discordId);
        Task<PlayerDto> RemoveSilence(long id);
        Task<PlayerDto> RemoveBan(long id);

        Task<PlayerDto> AddBan(long id, PlayerVipDto dto);
        Task<PlayerDto> AddDiscordRole(long id, PlayerVipDto dto);
        Task<PlayerDto> AddSilence(long id, PlayerVipDto dto);
        Task<PlayerDto> AddVip(long id, PlayerVipDto dto);
        Task<PlayerDto> UpdateCoins(long id, ChangeAmountDto dto);
        Task UpdateCoinsToAll(bool online, ChangeAmountDto dto);
        Task<PlayerDto> UpdateFame(long id, ChangeAmountDto dto);
        Task<PlayerDto> UpdateGold(long id, ChangeAmountDto dto);
        Task<PlayerDto> UpdateMoney(long id, ChangeAmountDto dto);

        Task<int> GetCount();
        Task<int> GetVipCount();
        Task<int> GetWhitelistCount();
    }
}
