using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class CacheService : ICacheService
    {
        private Dictionary<long, List<ScumPlayer>> _connectedPlayers;
        private readonly Dictionary<long, Queue<BotCommand>> _botCommandQueue;
        private readonly Dictionary<long, Queue<FileChangeCommand>> _fileChangeQueue;

        public CacheService()
        {
            _connectedPlayers = [];
            _botCommandQueue = [];
            _fileChangeQueue = [];
        }

        public List<ScumPlayer> GetConnectedPlayers(long serverId)
        {
            return _connectedPlayers[serverId];
        }

        public Queue<BotCommand> GetCommandQueue(long serverId)
        {
            return _botCommandQueue[serverId];
        }

        public Queue<FileChangeCommand> GetFileChangeQueue(long serverId)
        {
            return _fileChangeQueue[serverId];
        }

        public void ClearConnectedPlayers(long serverId)
        {
            _connectedPlayers[serverId] = [];
        }

        public void SetConnectedPlayers(long serverId, List<ScumPlayer> players)
        {
            _connectedPlayers[serverId] = players.DistinctBy(player => player.SteamID).ToList();
        }

        public void AddServers(List<Entities.ScumServer> servers)
        {
            foreach (var server in servers)
            {
                if (!_connectedPlayers.ContainsKey(server.Id))
                {
                    _connectedPlayers.Add(server.Id, []);
                }

                if (!_botCommandQueue.ContainsKey(server.Id))
                {
                    _botCommandQueue.Add(server.Id, []);
                }

                if (!_fileChangeQueue.ContainsKey(server.Id))
                {
                    _fileChangeQueue.Add(server.Id, []);
                }
            }
        }
    }
}
