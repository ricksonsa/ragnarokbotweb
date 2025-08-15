using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ICacheService
    {
        void ClearConnectedPlayers(long serverId);
        ConcurrentQueue<BotCommand> GetCommandQueue(long serverId);
        ConcurrentQueue<FileChangeCommand> GetFileChangeQueue(long serverId);
        List<Shared.Models.ScumPlayer> GetConnectedPlayers(long serverId);
        List<Shared.Models.ScumSquad> GetSquads(long serverId);
        List<Shared.Models.ScumFlag> GetFlags(long serverId);
        RaidTimes? GetRaidTimes(long serverId);
        void SetRaidTimes(long serverId, RaidTimes config);
        ConcurrentDictionary<Guid, BotUser> GetConnectedBots(long serverId);
        void SetConnectedPlayers(long serverId, List<Shared.Models.ScumPlayer> players);
        void SetSquads(long serverId, List<Shared.Models.ScumSquad> squads);
        void SetFlags(long serverId, List<Shared.Models.ScumFlag> flags);
        void AddServers(List<ScumServer> servers);
        int GetQueueCount(long serverId);
        void EnqueueCommand(long serverId, BotCommand command);
        bool TryDequeueCommand(long serverId, out BotCommand? command);
        List<BotCommand> DequeueAllCommands(long serverId);
        void EnqueueFileChangeCommand(long serverId, FileChangeCommand command);
        bool TryDequeueFileChangeCommand(long serverId, out FileChangeCommand? command);
        int GetFileChangeQueueCount(long serverId);
    }
}
