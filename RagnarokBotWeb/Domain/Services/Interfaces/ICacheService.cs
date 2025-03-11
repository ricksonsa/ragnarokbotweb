using RagnarokBotWeb.Application;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ICacheService
    {
        void ClearConnectedPlayers(long serverId);
        Queue<BotCommand> GetCommandQueue(long serverId);
        List<Shared.Models.ScumPlayer> GetConnectedPlayers(long serverId);
        void SetConnectedPlayers(long serverId, List<Shared.Models.ScumPlayer> players);
        void AddServers(List<ScumServer> servers);
    }
}
