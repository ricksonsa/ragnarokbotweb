using RagnarokBotWeb.Application;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class CacheService : ICacheService
    {
        private Dictionary<long, List<ScumPlayer>> _connectedPlayers;
        private readonly Dictionary<long, Queue<BotCommand>> _queue;

        public CacheService()
        {
            _connectedPlayers = [];
            _queue = [];
        }

        public List<ScumPlayer> GetConnectedPlayers(long serverId)
        {
            return _connectedPlayers[serverId];
        }

        public Queue<BotCommand> GetCommandQueue(long serverId)
        {
            return _queue[serverId];
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

                if (!_queue.ContainsKey(server.Id))
                {
                    _queue.Add(server.Id, []);
                }
            }
        }
    }
}
