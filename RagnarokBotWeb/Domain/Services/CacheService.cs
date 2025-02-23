using RagnarokBotWeb.Application;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class CacheService : ICacheService
    {
        private Dictionary<string, ScumPlayer> _connectedUsers;
        private readonly Queue<BotCommand> _commandQueue;

        public CacheService()
        {
            _connectedUsers = [];
            _commandQueue = [];
        }

        public Dictionary<string, ScumPlayer> GetConnectedPlayers()
        {
            return _connectedUsers;
        }

        public Queue<BotCommand> GetCommandQueue()
        {
            return _commandQueue;
        }

        public void ClearConnectedPlayers()
        {
            _connectedUsers = [];
        }

        public void SetConnectedPlayers(List<ScumPlayer> players)
        {
            _connectedUsers = players.DistinctBy(player => player.SteamID).ToDictionary(value => value.SteamID);
        }
    }
}
