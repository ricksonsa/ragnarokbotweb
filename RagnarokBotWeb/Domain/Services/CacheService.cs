using RagnarokBotWeb.Application;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Domain.Services
{
    public class CacheService : ICacheService
    {
        private readonly Dictionary<long, Queue<BotCommand>> _botCommandQueue;
        private readonly Dictionary<long, Queue<FileChangeCommand>> _fileChangeQueue;

        private Dictionary<long, Dictionary<Guid, BotUser>> _connectedBots;
        private Dictionary<long, List<ScumPlayer>> _connectedPlayers;
        private Dictionary<long, List<ScumSquad>> _squads;
        private Dictionary<long, List<ScumFlag>> _flags;
        private Dictionary<long, RaidTimes?> _raidTimes;

        public CacheService()
        {
            _connectedPlayers = [];
            _botCommandQueue = [];
            _fileChangeQueue = [];
            _connectedBots = [];
            _raidTimes = [];
            _squads = [];
            _flags = [];
        }

        public List<ScumPlayer> GetConnectedPlayers(long serverId)
        {
            return _connectedPlayers[serverId];
        }

        public RaidTimes? GetRaidTimes(long serverId)
        {
            return _raidTimes[serverId];
        }

        public List<ScumSquad> GetSquads(long serverId)
        {
            return _squads[serverId];
        }

        public List<ScumFlag> GetFlags(long serverId)
        {
            return _flags[serverId];
        }

        public void SetRaidTimes(long serverId, RaidTimes config)
        {
            _raidTimes[serverId] = config;
        }

        public void SetSquads(long serverId, List<ScumSquad> squads)
        {
            _squads[serverId] = squads;
        }

        public void SetFlags(long serverId, List<ScumFlag> flags)
        {
            _flags[serverId] = flags;
        }

        public Dictionary<Guid, BotUser> GetConnectedBots(long serverId)
        {
            return _connectedBots[serverId];
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
            _connectedPlayers[serverId] = players;
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

                if (!_connectedBots.ContainsKey(server.Id))
                {
                    _connectedBots.Add(server.Id, []);
                }

                if (!_squads.ContainsKey(server.Id))
                {
                    _squads.Add(server.Id, []);
                }

                if (!_flags.ContainsKey(server.Id))
                {
                    _flags.Add(server.Id, []);
                }

                if (!_raidTimes.ContainsKey(server.Id))
                {
                    _raidTimes.Add(server.Id, null);
                }

            }
        }
    }
}
