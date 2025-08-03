using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ICacheService
    {
        void ClearConnectedPlayers(long serverId);
        Queue<BotCommand> GetCommandQueue(long serverId);
        Queue<FileChangeCommand> GetFileChangeQueue(long serverId);
        List<Shared.Models.ScumPlayer> GetConnectedPlayers(long serverId);
        List<Shared.Models.Squad> GetSquads(long serverId);
        RaidTimes? GetRaidTimes(long serverId);
        void SetRaidTimes(long serverId, RaidTimes config);
        Dictionary<Guid, BotUser> GetConnectedBots(long serverId);
        void SetConnectedPlayers(long serverId, List<Shared.Models.ScumPlayer> players);
        void SetSquads(long serverId, List<Shared.Models.Squad> squads);
        void AddServers(List<ScumServer> servers);
    }
}
