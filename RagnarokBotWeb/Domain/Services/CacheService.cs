using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class CacheService : ICacheService
    {
        private Dictionary<string, Player> _connectedUsers;
        private readonly Queue<Command> _commandQueue;

        public CacheService()
        {
            _connectedUsers = [];
            _commandQueue = [];
        }

        public Dictionary<string, Player> GetConnectedPlayers()
        {
            return _connectedUsers;
        }

        public Queue<Command> GetCommandQueue()
        {
            return _commandQueue;
        }

        public void ClearConnectedPlayers()
        {
            _connectedUsers = [];
        }

        public void SetConnectedPlayers(List<Player> players)
        {
            _connectedUsers = players.DistinctBy(player => player.SteamID).ToDictionary(value => value.SteamID);
        }
    }
}
