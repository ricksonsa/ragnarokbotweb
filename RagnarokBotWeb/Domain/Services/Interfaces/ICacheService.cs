using RagnarokBotWeb.Application.Models;
using Shared.Models;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ICacheService
    {
        void ClearConnectedPlayers(long serverId);
        ConcurrentQueue<BotCommand> GetCommandQueue(long serverId);
        ConcurrentQueue<FileChangeCommand> GetFileChangeQueue(long serverId);
        List<ScumPlayer> GetConnectedPlayers(long serverId);
        List<ScumSquad> GetSquads(long serverId);
        List<ScumFlag> GetFlags(long serverId);
        RaidTimes? GetRaidTimes(long serverId);
        void SetRaidTimes(long serverId, RaidTimes config);
        void SetConnectedPlayers(long serverId, List<ScumPlayer> players);
        void SetSquads(long serverId, List<ScumSquad> squads);
        void SetFlags(long serverId, List<ScumFlag> flags);
        void AddServers(List<Entities.ScumServer> servers);
        int GetQueueCount(long serverId);
        void EnqueueCommand(long serverId, BotCommand command);
        bool TryDequeueCommand(long serverId, out BotCommand? command);
        List<BotCommand> DequeueAllCommands(long serverId);
        void EnqueueFileChangeCommand(long serverId, FileChangeCommand command);
        bool TryDequeueFileChangeCommand(long serverId, out FileChangeCommand? command);
        int GetFileChangeQueueCount(long serverId);
    }
}
