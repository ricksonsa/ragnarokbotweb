using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Domain.Services
{
    public class CacheService : ICacheService
    {
        // Thread-safe collections for queues and bots
        private readonly ConcurrentDictionary<long, ConcurrentQueue<BotCommand>> _botCommandQueue;
        private readonly ConcurrentDictionary<long, ConcurrentQueue<FileChangeCommand>> _fileChangeQueue;
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<Guid, BotUser>> _connectedBots;

        // For collections that get replaced entirely, keep as regular collections but use locks
        private readonly ConcurrentDictionary<long, List<ScumPlayer>> _connectedPlayers;
        private readonly ConcurrentDictionary<long, List<ScumSquad>> _squads;
        private readonly ConcurrentDictionary<long, List<ScumFlag>> _flags;
        private readonly ConcurrentDictionary<long, RaidTimes?> _raidTimes;

        // Locks for list operations
        private readonly ConcurrentDictionary<long, object> _playerLocks = new();

        private readonly Guid _instanceId;
        private static int _instanceCounter = 0;
        private readonly ILogger<CacheService> _logger;

        public CacheService(ILogger<CacheService> logger)
        {
            _logger = logger;
            _instanceId = Guid.NewGuid();
            var instanceNumber = Interlocked.Increment(ref _instanceCounter);

            _botCommandQueue = new ConcurrentDictionary<long, ConcurrentQueue<BotCommand>>();
            _fileChangeQueue = new ConcurrentDictionary<long, ConcurrentQueue<FileChangeCommand>>();
            _connectedBots = new ConcurrentDictionary<long, ConcurrentDictionary<Guid, BotUser>>();
            _connectedPlayers = new ConcurrentDictionary<long, List<ScumPlayer>>();
            _squads = new ConcurrentDictionary<long, List<ScumSquad>>();
            _flags = new ConcurrentDictionary<long, List<ScumFlag>>();
            _raidTimes = new ConcurrentDictionary<long, RaidTimes?>();

            _logger.LogInformation("CacheService instance #{InstanceNumber} created with ID: {InstanceId}", instanceNumber, _instanceId);
        }

        public List<ScumPlayer> GetConnectedPlayers(long serverId)
        {
            var lockObj = _playerLocks.GetOrAdd(serverId, _ => new object());
            lock (lockObj)
            {
                return _connectedPlayers.TryGetValue(serverId, out var players)
                    ? new List<ScumPlayer>(players)
                    : new List<ScumPlayer>();
            }
        }

        public RaidTimes? GetRaidTimes(long serverId)
        {
            return _raidTimes.TryGetValue(serverId, out var raidTimes) ? raidTimes : null;
        }

        public List<ScumSquad> GetSquads(long serverId)
        {
            return _squads.TryGetValue(serverId, out var squads)
                ? new List<ScumSquad>(squads)
                : new List<ScumSquad>();
        }

        public List<ScumFlag> GetFlags(long serverId)
        {
            return _flags.TryGetValue(serverId, out var flags)
                ? new List<ScumFlag>(flags)
                : new List<ScumFlag>();
        }

        public void SetRaidTimes(long serverId, RaidTimes config)
        {
            _raidTimes.AddOrUpdate(serverId, config, (key, oldValue) => config);
        }

        public void SetSquads(long serverId, List<ScumSquad> squads)
        {
            _squads.AddOrUpdate(serverId, new List<ScumSquad>(squads), (key, oldValue) => new List<ScumSquad>(squads));
        }

        public void SetFlags(long serverId, List<ScumFlag> flags)
        {
            _flags.AddOrUpdate(serverId, new List<ScumFlag>(flags), (key, oldValue) => new List<ScumFlag>(flags));
        }

        public ConcurrentDictionary<Guid, BotUser> GetConnectedBots(long serverId)
        {
            return _connectedBots.GetOrAdd(serverId, _ => new ConcurrentDictionary<Guid, BotUser>());
        }

        // Thread-safe queue operations
        public ConcurrentQueue<BotCommand> GetCommandQueue(long serverId)
        {
            return _botCommandQueue.GetOrAdd(serverId, _ => new ConcurrentQueue<BotCommand>());
        }

        public ConcurrentQueue<FileChangeCommand> GetFileChangeQueue(long serverId)
        {
            return _fileChangeQueue.GetOrAdd(serverId, _ => new ConcurrentQueue<FileChangeCommand>());
        }

        public void ClearConnectedPlayers(long serverId)
        {
            var lockObj = _playerLocks.GetOrAdd(serverId, _ => new object());
            lock (lockObj)
            {
                _connectedPlayers.AddOrUpdate(serverId, new List<ScumPlayer>(), (key, oldValue) => new List<ScumPlayer>());
            }
        }

        public void SetConnectedPlayers(long serverId, List<ScumPlayer> players)
        {
            var lockObj = _playerLocks.GetOrAdd(serverId, _ => new object());
            lock (lockObj)
            {
                _connectedPlayers.AddOrUpdate(serverId, new List<ScumPlayer>(players), (key, oldValue) => new List<ScumPlayer>(players));
            }
        }

        public void AddServers(List<Entities.ScumServer> servers)
        {
            foreach (var server in servers)
            {
                _botCommandQueue.TryAdd(server.Id, new ConcurrentQueue<BotCommand>());
                _fileChangeQueue.TryAdd(server.Id, new ConcurrentQueue<FileChangeCommand>());
                _connectedBots.TryAdd(server.Id, new ConcurrentDictionary<Guid, BotUser>());
                _connectedPlayers.TryAdd(server.Id, new List<ScumPlayer>());
                _squads.TryAdd(server.Id, new List<ScumSquad>());
                _flags.TryAdd(server.Id, new List<ScumFlag>());
                _raidTimes.TryAdd(server.Id, null);
                _playerLocks.TryAdd(server.Id, new object());
            }
        }

        // Thread-safe helper methods
        public int GetQueueCount(long serverId)
        {
            var queue = GetCommandQueue(serverId);
            return queue.Count;
        }

        public void LogQueueState(long serverId, string context)
        {
            var queue = GetCommandQueue(serverId);
            _logger.LogDebug("[{Context}] Instance {InstanceId} - Server {ServerId}: Queue count = {QueueCount}",
                context, _instanceId, serverId, queue.Count);
        }

        public void EnqueueCommand(long serverId, BotCommand command)
        {
            var queue = GetCommandQueue(serverId);
            queue.Enqueue(command);
            _logger.LogDebug("Enqueued command for server {ServerId}. Queue count: {QueueCount}", serverId, queue.Count);
        }

        public bool TryDequeueCommand(long serverId, out BotCommand? command)
        {
            var queue = GetCommandQueue(serverId);
            var queueCountBefore = queue.Count;

            var result = queue.TryDequeue(out command);
            if (result)
            {
                _logger.LogDebug("Dequeued command for server {ServerId}. Queue count: {QueueCountBefore} -> {QueueCountAfter}",
                    serverId, queueCountBefore, queue.Count);
            }
            else
            {
                _logger.LogDebug("No commands to dequeue for server {ServerId}. Queue count: {QueueCount}",
                    serverId, queue.Count);
            }
            return result;
        }

        // Batch dequeue for better performance
        public List<BotCommand> DequeueAllCommands(long serverId)
        {
            var queue = GetCommandQueue(serverId);
            var commands = new List<BotCommand>();
            var initialCount = queue.Count;

            while (queue.TryDequeue(out var command))
            {
                commands.Add(command);
            }

            _logger.LogInformation("Batch dequeue for server {ServerId}: {CommandCount} commands dequeued (from {InitialCount})",
                serverId, commands.Count, initialCount);
            return commands;
        }

        public void EnqueueFileChangeCommand(long serverId, FileChangeCommand command)
        {
            var queue = GetFileChangeQueue(serverId);
            queue.Enqueue(command);
        }

        public bool TryDequeueFileChangeCommand(long serverId, out FileChangeCommand? command)
        {
            var queue = GetFileChangeQueue(serverId);
            return queue.TryDequeue(out command);
        }

        public int GetFileChangeQueueCount(long serverId)
        {
            var queue = GetFileChangeQueue(serverId);
            return queue.Count;
        }
    }
}