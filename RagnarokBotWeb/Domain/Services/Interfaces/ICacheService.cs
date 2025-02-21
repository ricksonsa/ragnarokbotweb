using RagnarokBotWeb.Domain.Entities;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ICacheService
    {
        void ClearConnectedPlayers();
        Queue<Command> GetCommandQueue();
        Dictionary<string, Player> GetConnectedPlayers();
        void SetConnectedPlayers(List<Player> players);
    }
}
