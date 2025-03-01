using RagnarokBotWeb.Application;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ICacheService
    {
        void ClearConnectedPlayers(long serverId);
        Queue<BotCommand> GetCommandQueue(long serverId);
        List<ScumPlayer> GetConnectedPlayers(long serverId);
        void SetConnectedPlayers(long serverId, List<ScumPlayer> players);
    }
}
