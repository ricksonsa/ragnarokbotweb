using RagnarokBotWeb.Application;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ICacheService
    {
        void ClearConnectedPlayers();
        Queue<BotCommand> GetCommandQueue();
        Dictionary<string, ScumPlayer> GetConnectedPlayers();
        void SetConnectedPlayers(List<ScumPlayer> players);
    }
}
