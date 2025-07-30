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
        List<Guid> GetConnectedBots(long serverId);
        void SetConnectedPlayers(long serverId, List<Shared.Models.ScumPlayer> players);
        void AddServers(List<ScumServer> servers);
    }
}
